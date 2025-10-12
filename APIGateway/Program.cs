using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Gateway", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Use a single combined swagger document served by the gateway
    c.SwaggerEndpoint("/swagger/combined/swagger.json", "Combined API");
    c.RoutePrefix = "swagger";
});

// Aggregated swagger JSON endpoint: fetch downstream swagger.json files and merge them
app.MapGet("/swagger/combined/swagger.json", async (IHttpClientFactory httpFactory) =>
{
    var client = httpFactory.CreateClient();
    var merged = new JsonObject();

    // Basic info for combined doc
    merged["openapi"] = "3.0.1";
    merged["info"] = new JsonObject
    {
        ["title"] = "Combined API",
        ["version"] = "v1",
        ["description"] = "Aggregated OpenAPI document from downstream services"
    };
    merged["servers"] = new JsonArray(new JsonObject { ["url"] = "/" });

    JsonObject paths = new JsonObject();
    JsonObject components = new JsonObject();

    var failed = new List<string>();
    async Task TryFetchAndMerge(string url)
    {
        try
        {
            var json = await client.GetStringAsync(url);
            var node = JsonNode.Parse(json)?.AsObject();
            if (node == null) return;

            if (node.TryGetPropertyValue("paths", out var p) && p is JsonObject pObj)
            {
                foreach (var kv in pObj)
                {
                    // If path already exists, skip or rename — for dev we skip duplicates
                    if (!paths.ContainsKey(kv.Key)) paths[kv.Key] = kv.Value;
                }
            }

            if (node.TryGetPropertyValue("components", out var c) && c is JsonObject cObj)
            {
                foreach (var compKind in cObj)
                {
                    if (!components.ContainsKey(compKind.Key))
                    {
                        components[compKind.Key] = compKind.Value;
                    }
                    else if (components[compKind.Key] is JsonObject existing && compKind.Value is JsonObject incoming)
                    {
                        foreach (var inner in incoming)
                        {
                            if (!existing.ContainsKey(inner.Key)) existing[inner.Key] = inner.Value;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // record failed url and minimal log for debugging
            failed.Add(url + " -> " + ex.Message);
            try { Console.WriteLine($"[Gateway] Failed to fetch swagger from {url}: {ex.Message}"); } catch { }
        }
    }

    // Downstream swagger URLs (use container DNS names)
    await TryFetchAndMerge("http://authservice:5133/swagger/v1/swagger.json");
    await TryFetchAndMerge("http://productservice:5137/swagger/v1/swagger.json");

    merged["paths"] = paths;
    if (components.Count > 0) merged["components"] = components;
    if (failed.Count > 0)
    {
        merged["x-service-errors"] = new JsonArray(failed.Select(f => JsonValue.Create(f)).ToArray());
    }

    return Results.Json(merged);
});

// Diagnostic endpoint: probe downstream swagger endpoints and return per-service status
app.MapGet("/swagger/status", async (IHttpClientFactory httpFactory) =>
{
    var client = httpFactory.CreateClient();
    var targets = new Dictionary<string, string>
    {
        ["auth"] = "http://authservice:5133/swagger/v1/swagger.json",
        ["product"] = "http://productservice:5137/swagger/v1/swagger.json"
    };

    var results = new JsonObject();

    foreach (var kv in targets)
    {
        try
        {
            using var resp = await client.GetAsync(kv.Value);
            results[kv.Key] = new JsonObject
            {
                ["ok"] = resp.IsSuccessStatusCode,
                ["statusCode"] = (int)resp.StatusCode,
                ["reason"] = resp.ReasonPhrase
            };
        }
        catch (Exception ex)
        {
            results[kv.Key] = new JsonObject
            {
                ["ok"] = false,
                ["error"] = ex.Message
            };
            try { Console.WriteLine($"[Gateway] Probe failed for {kv.Key} ({kv.Value}): {ex.Message}"); } catch { }
        }
    }

    return Results.Json(results);
});

app.MapReverseProxy();

// Health endpoint for orchestration / load balancers
app.MapGet("/health", () => Results.Json(new { status = "ok", service = "APIGateway" }));

app.Run();
