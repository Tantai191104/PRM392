using Microsoft.OpenApi.Models;
using System.IO;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Load YARP Reverse Proxy configuration from appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Basic gateway settings (can be overridden by environment variables)
// - Gateway:ForwardAllHeaders (bool) : forward all incoming headers to downstream (dangerous in prod)
// - Gateway:ForwardAuthHeader (bool) : forward Authorization header when false it will be removed
// - Gateway:CorsAllowOrigins (array) : optional list of allowed origins for CORS in dev

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Gateway", Version = "v1" });
});

// Persist DataProtection keys to disk so containers can be restarted safely (volume mount expected at /keys)
try
{
    var keysPath = Path.Combine(Path.GetTempPath(), "apigateway-keys");
    // If running in container prefer /keys path
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
    // DataProtection persistence is best-effort; don't break the app if it fails
    Console.WriteLine($"Warning: could not configure DataProtection key persistence: {ex.Message}");
}

// CORS - permissive for local development; consider restricting in production
builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayCors", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

var config = app.Configuration;

// Read downstream swagger mapping from configuration (appsettings.json or env overrides)
var downstream = config.GetSection("DownstreamSwagger").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

// Read gateway header forwarding settings
var forwardAll = config.GetValue<bool?>("Gateway:ForwardAllHeaders") ?? false;
var forwardAuth = config.GetValue<bool?>("Gateway:ForwardAuthHeader") ?? true;

app.UseRouting();
app.UseCors("GatewayCors");

// Simple middleware to control forwarding of the Authorization header.
// This avoids mixing complex YARP transforms and provides a clear config toggle.
app.Use(async (context, next) =>
{
    if (!forwardAll && !forwardAuth)
    {
        // Remove Authorization header before YARP forwards the request
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            context.Request.Headers.Remove("Authorization");
        }
    }

    // Continue to next middleware (YARP will run later)
    await next();
});

// Expose Swagger UI and dynamically register downstream swagger endpoints served via the gateway proxy
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Register gateway's own doc
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");

    // For each downstream service configured, publish a Swagger UI entry that points to the proxied path
    foreach (var kv in downstream)
    {
        // Example: service key 'auth' -> proxied swagger path '/api/auth/swagger/v1/swagger.json'
        var serviceName = kv.Key;
        var proxiedUrl = $"/api/{serviceName}/swagger/v1/swagger.json";
        c.SwaggerEndpoint(proxiedUrl, $"{serviceName} (proxied)");
    }

    c.RoutePrefix = "swagger";
});

// Map health endpoints
app.MapGet("/health", () => Results.Json(new { status = "ok", service = "APIGateway" }));
app.MapGet("/ready", () => Results.Json(new { status = "ready" }));

// Map YARP reverse proxy; routes are configured in appsettings.json
app.MapReverseProxy();

// Log effective configuration at startup for easier diagnostics
app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine("APIGateway started with the following downstream mappings:");
    foreach (var kv in downstream)
    {
        Console.WriteLine($" - {kv.Key} => {kv.Value}");
    }
    Console.WriteLine($"Gateway forwarding: ForwardAllHeaders={forwardAll}, ForwardAuthHeader={forwardAuth}");
});

app.Run();
