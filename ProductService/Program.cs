using ProductService.Infrastructure.Repositories;
using ProductService.Application.Services;
using MongoDB.Driver;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===== Load config =====
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// ===== MongoDB =====
var mongoSettings = builder.Configuration.GetSection("MongoSettings");
var client = new MongoClient(mongoSettings["ConnectionString"]);
var dbName = mongoSettings["DatabaseName"]!;
var database = client.GetDatabase(dbName);

// ===== DI =====
builder.Services.AddSingleton<IMongoClient>(client);
builder.Services.AddSingleton<IMongoDatabase>(database);
builder.Services.AddSingleton(new ProductRepository(client, dbName));
builder.Services.AddSingleton<ProductAppService>();

// ✅ GeminiService với API Key
builder.Services.AddHttpClient("gemini");
builder.Services.AddSingleton<GeminiService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("gemini");
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<GeminiService>>();

    // Endpoint chuẩn dùng API Key
    var endpoint = config["Gemini:Endpoint"] ??
                   throw new Exception("Missing Gemini:Endpoint");
    var apiKey = config["Gemini:ApiKey"] ??
                 throw new Exception("Missing Gemini:ApiKey");

    // Thêm API Key vào query string
    httpClient.BaseAddress = new Uri($"{endpoint}?key={apiKey}");

    return new GeminiService(httpClient, config["Gemini:Endpoint"] ?? throw new Exception("Missing Gemini:Endpoint"), config["Gemini:ApiKey"] ?? throw new Exception("Missing Gemini:ApiKey"), logger);
});

// Price Suggestion Service
builder.Services.AddSingleton<IPriceSuggestionService, PriceSuggestionService>();

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

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

// ===== JWT Auth =====
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                Console.WriteLine("✅ Token validated for ProductService");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"❌ Token validation failed: {ctx.Exception?.Message}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ===== Downstream services =====
builder.Services.AddHttpClient("auth", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Downstream:AuthBaseUrl"] ?? "http://authservice:5133");
});

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5000", "http://127.0.0.1:5000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ===== Middleware =====
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductService API V1");
    c.RoutePrefix = string.Empty;
});

// app.UseHttpsRedirection();
app.MapControllers();
app.MapGet("/health", () => Results.Json(new { status = "ok", service = "ProductService" }));

app.Run();
