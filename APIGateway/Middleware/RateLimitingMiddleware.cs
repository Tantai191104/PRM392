using Microsoft.Extensions.Caching.Distributed;
using System.Net;

namespace APIGateway.Middleware
{
    /// <summary>
    /// Rate limiting middleware to prevent API abuse
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDistributedCache _cache;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly int _requestLimit;
        private readonly TimeSpan _timeWindow;

        public RateLimitingMiddleware(
            RequestDelegate next,
            IDistributedCache cache,
            ILogger<RateLimitingMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
            _requestLimit = configuration.GetValue<int>("RateLimit:RequestLimit", 100);
            _timeWindow = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimit:TimeWindowMinutes", 1));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = GetClientIpAddress(context);
            var key = $"rate_limit:{ipAddress}";

            try
            {
                var currentCount = await GetRequestCountAsync(key);

                if (currentCount >= _requestLimit)
                {
                    _logger.LogWarning("Rate limit exceeded for IP: {IpAddress}", ipAddress);
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.Headers.Add("X-Rate-Limit-Limit", _requestLimit.ToString());
                    context.Response.Headers.Add("X-Rate-Limit-Remaining", "0");
                    context.Response.Headers.Add("X-Rate-Limit-Reset", DateTimeOffset.UtcNow.Add(_timeWindow).ToUnixTimeSeconds().ToString());
                    
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = "Too many requests. Please try again later.",
                        retryAfter = (int)_timeWindow.TotalSeconds
                    });
                    return;
                }

                await IncrementRequestCountAsync(key);

                // Add rate limit headers
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.Add("X-Rate-Limit-Limit", _requestLimit.ToString());
                    context.Response.Headers.Add("X-Rate-Limit-Remaining", (_requestLimit - currentCount - 1).ToString());
                    context.Response.Headers.Add("X-Rate-Limit-Reset", DateTimeOffset.UtcNow.Add(_timeWindow).ToUnixTimeSeconds().ToString());
                    return Task.CompletedTask;
                });

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in rate limiting middleware for IP: {IpAddress}", ipAddress);
                // Continue processing even if rate limiting fails
                await _next(context);
            }
        }

        private async Task<int> GetRequestCountAsync(string key)
        {
            var value = await _cache.GetStringAsync(key);
            return string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
        }

        private async Task IncrementRequestCountAsync(string key)
        {
            var currentCount = await GetRequestCountAsync(key);
            var newCount = currentCount + 1;
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _timeWindow
            };
            
            await _cache.SetStringAsync(key, newCount.ToString(), options);
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Check for X-Forwarded-For header (common in reverse proxy scenarios)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            // Check for X-Real-IP header
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fallback to RemoteIpAddress
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
