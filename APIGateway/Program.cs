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
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer {token}' to authorize requests"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
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

// CORS - allow multiple domains from environment variable
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

// Read downstream swagger mapping from configuration (appsettings.json or env overrides)
var downstream = config.GetSection("DownstreamSwagger").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

// Read gateway header forwarding settings
var forwardAll = config.GetValue<bool?>("Gateway:ForwardAllHeaders") ?? false;
var forwardAuth = config.GetValue<bool?>("Gateway:ForwardAuthHeader") ?? true;

app.UseRouting();
app.UseCors("GatewayCors");

// Simple middleware to control forwarding of the Authorization header.
// This avoids mixing complex YARP transforms and provides a clear config toggle.
// Middleware: validate incoming JWT and inject user headers for downstream
app.Use(async (context, next) =>
{
    // If there's an Authorization header and gateway is allowed to forward auth
    if (context.Request.Headers.TryGetValue("Authorization", out var auth) && forwardAuth)
    {
        var token = auth.ToString().Split(' ').LastOrDefault();
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var jwtSettings = config.GetSection("JwtSettings");
                var key = System.Text.Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
                var validations = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, validations, out var validatedToken);

                // If validation succeeded, inject user info headers
                var sub = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                var email = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;
                if (!string.IsNullOrEmpty(sub))
                {
                    context.Request.Headers["X-User-Id"] = sub;
                    Console.WriteLine($"APIGateway: injected X-User-Id={sub}");
                }
                if (!string.IsNullOrEmpty(email)) context.Request.Headers["X-User-Email"] = email;
            }
            catch (Exception ex)
            {
                // Log validation failure and attempt best-effort parse (dev fallback)
                Console.WriteLine($"APIGateway: token validation failed: {ex.Message}");
                try
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(token);
                    var sub = jwt.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                    var email = jwt.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;
                    if (!string.IsNullOrEmpty(sub))
                    {
                        context.Request.Headers["X-User-Id"] = sub;
                        Console.WriteLine($"APIGateway: best-effort injected X-User-Id={sub}");
                    }
                    if (!string.IsNullOrEmpty(email)) context.Request.Headers["X-User-Email"] = email;
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"APIGateway: failed to read token without validation: {ex2.Message}");
                }
            }
        }
    }

    // Respect forwarding options: if neither forwardAll nor forwardAuth -> strip Authorization
    if (!forwardAll && !forwardAuth)
    {
        if (context.Request.Headers.ContainsKey("Authorization")) context.Request.Headers.Remove("Authorization");
    }

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
