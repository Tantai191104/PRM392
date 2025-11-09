using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace APIGateway.Services
{
    /// <summary>
    /// Caching service using Redis for distributed caching
    /// </summary>
    public class CacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Get cached data
        /// </summary>
        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(key);
                if (string.IsNullOrEmpty(cachedData))
                    return default;

                return JsonSerializer.Deserialize<T>(cachedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache for key: {Key}", key);
                return default;
            }
        }

        /// <summary>
        /// Set cache with expiration
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var serializedData = JsonSerializer.Serialize(value);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
                };

                await _cache.SetStringAsync(key, serializedData, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache for key: {Key}", key);
            }
        }

        /// <summary>
        /// Remove cached data
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            }
        }

        /// <summary>
        /// Get or create cache - fetch from cache or execute factory and cache result
        /// </summary>
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            try
            {
                var cachedData = await GetAsync<T>(key);
                if (cachedData != null)
                {
                    _logger.LogInformation("Cache hit for key: {Key}", key);
                    return cachedData;
                }

                _logger.LogInformation("Cache miss for key: {Key}", key);
                var data = await factory();
                await SetAsync(key, data, expiration);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrCreate for key: {Key}", key);
                // If caching fails, still return the data
                return await factory();
            }
        }

        /// <summary>
        /// Clear cache by pattern (for Redis)
        /// </summary>
        public async Task ClearByPatternAsync(string pattern)
        {
            try
            {
                // Note: This requires Redis-specific implementation
                // For now, just log it
                _logger.LogInformation("Clear cache pattern requested: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache by pattern: {Pattern}", pattern);
            }
        }
    }
}
