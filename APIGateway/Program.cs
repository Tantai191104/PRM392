using Microsoft.OpenApi.Models;
using System.IO;
using Microsoft.AspNetCore.DataProtection;
using System.Collections.Concurrent;
using APIGateway.Services;
using APIGateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

// =============================
// REDIS CACHE
// =============================
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "PRM392:";
});

// =============================
// CACHE SERVICE
// =============================
builder.Services.AddSingleton<CacheService>();

// =============================
// YARP Reverse Proxy
// =============================
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// =============================
// Dashboard Service
// =============================
builder.Services.AddHttpClient<DashboardService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddControllers();

// =============================
// Swagger / OpenAPI
// =============================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Gateway", Version = "v1" });
});

// =============================
// Persist DataProtection keys
// =============================
try
{
    var keysPath = Path.Combine(Path.GetTempPath(), "apigateway-keys");
    if (Directory.Exists("/keys") || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
    {
        keysPath = "/keys";
    }

    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("PRM392_Gateway");
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: could not configure DataProtection key persistence: {ex.Message}");
}

// =============================
// CORS
// =============================
var allowedOrigins = builder.Configuration.GetValue<string>("Gateway__CorsAllowOrigins")?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayCors", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        else
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
    });
});

var app = builder.Build();

var config = app.Configuration;
var downstream = config.GetSection("DownstreamSwagger").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var forwardAll = config.GetValue<bool?>("Gateway:ForwardAllHeaders") ?? false;

// =============================
// Middleware
// =============================
app.UseRouting();
app.UseCors("GatewayCors");

// Map controllers for Dashboard API
app.MapControllers();

// ---- Rate limiting per IP ----
var rateLimitWindow = TimeSpan.FromMinutes(1);
var rateLimitMaxRequests = 100;
var ipRequestCounters = new ConcurrentDictionary<string, (int Count, DateTime WindowStart)>();

app.Use(async (context, next) =>
{
    var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var now = DateTime.UtcNow;

    ipRequestCounters.AddOrUpdate(remoteIp,
        _ => (1, now),
        (_, old) =>
        {
            if ((now - old.WindowStart) > rateLimitWindow)
                return (1, now);
            else
                return (old.Count + 1, old.WindowStart);
        });

    if (ipRequestCounters[remoteIp].Count > rateLimitMaxRequests)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.Response.WriteAsync("Too Many Requests: rate limit exceeded.");
        Console.WriteLine($"Rate limit exceeded for IP {remoteIp} ({ipRequestCounters[remoteIp].Count} requests)");
        return;
    }

    await next();
});

// =============================
// RATE LIMITING MIDDLEWARE
// =============================
var enableRateLimiting = builder.Configuration.GetValue<bool>("RateLimit:EnableRateLimiting", true);
if (enableRateLimiting)
{
    app.UseMiddleware<RateLimitingMiddleware>();
    app.Logger.LogInformation("Rate limiting is ENABLED");
}
else
{
    app.Logger.LogInformation("Rate limiting is DISABLED");
}

// =============================
// Swagger UI
// =============================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    c.DefaultModelsExpandDepth(-1);

    // APIGateway's own Dashboard API (MUST BE FIRST)
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway - Dashboard");

    // Các service downstream hiển thị trong UI
    var downstreamList = downstream
        .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
        .Select(kv => new
        {
            Key = kv.Key,
            Url = $"/api/{kv.Key}/swagger/v1/swagger.json"
        }).ToList();

    string FriendlyLabel(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return key;
        var parts = key.Replace('-', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++) parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
        return string.Join(' ', parts);
    }

    foreach (var item in downstreamList)
    {
        c.SwaggerEndpoint(item.Url, $"{FriendlyLabel(item.Key)} (proxied)");
    }
});

// =============================
// Health endpoints
// =============================
app.MapGet("/health", () => Results.Json(new { status = "ok", service = "APIGateway" }));
app.MapGet("/ready", () => Results.Json(new { status = "ready" }));

// =============================
// YARP Reverse Proxy
// =============================
app.MapReverseProxy();

// =============================
// Startup logs
// =============================
app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine("APIGateway started with the following downstream mappings:");
    foreach (var kv in downstream)
    {
        Console.WriteLine($" - {kv.Key} => {kv.Value}");
    }
    Console.WriteLine($"Gateway forwarding: ForwardAllHeaders={forwardAll}");
});

app.Run();
