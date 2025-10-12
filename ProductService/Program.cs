using ProductService.Infrastructure.Repositories;
using ProductService.Application.Services;
using MongoDB.Driver;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== MongoDB =====
var mongoSettings = builder.Configuration.GetSection("MongoSettings");
var client = new MongoClient(mongoSettings["ConnectionString"]);
string dbName = mongoSettings["DatabaseName"]!;

// ===== DI =====
builder.Services.AddSingleton(new ProductRepository(client, dbName));
builder.Services.AddSingleton<ProductAppService>();

// ===== Controllers + Swagger =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ProductService API",
        Version = "v1",
        Description = "API for managing products"
    });
});

// ✅ ===== CORS phải đăng ký trước khi Build =====
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5016", "http://127.0.0.1:5016") // Gateway
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ✅ Bây giờ mới Build
var app = builder.Build();

// ===== Middleware =====
app.UseCors(); // Cho phép CORS
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductService API V1");
    c.RoutePrefix = string.Empty;
});

// app.UseHttpsRedirection(); // ❌ Bỏ khi chạy Docker
app.MapControllers();
// Health endpoint for orchestration / load balancers
app.MapGet("/health", () => Results.Json(new { status = "ok", service = "ProductService" }));
app.Run();
