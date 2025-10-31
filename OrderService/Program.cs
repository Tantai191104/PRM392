using OrderService.Application.Services;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.ExternalServices;
using OrderService.Infrastructure.Configuration;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// Đăng ký WalletServiceClient với HttpClient và cấu hình BaseAddress
var walletServiceBaseUrl = builder.Configuration["ExternalServices:WalletService:BaseUrl"] ?? "http://walletservice:5150";
builder.Services.AddHttpClient<WalletServiceClient>(client =>
{
    client.BaseAddress = new Uri(walletServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ==============================
// 📘 Add core services
// ==============================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Order Service API",
        Version = "v1",
        Description = "API for managing orders"
    });

    // ✅ JWT Bearer setup for Swagger UI
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer {token}' - you can copy the token from AuthService login response."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// ==============================
// ⚙️ Configuration Bindings
// ==============================
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoSettings"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// ==============================
// 🔐 JWT Authentication
// ==============================
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings?.Issuer,
            ValidAudience = jwtSettings?.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? "")),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

// ==============================
// 🛡 Authorization
// ==============================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrderPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin", "Staff", "User");
    });
});

// ==============================
// 🧩 FluentValidation
// ==============================
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// ==============================
// 💾 Dependency Injection
// ==============================
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderAppService, OrderAppService>();
builder.Services.AddScoped<EscrowServiceClient>();

// ==============================
// 🌐 External HTTP Clients
// ==============================

// Auth Service
builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    var baseUrl = builder.Configuration["ExternalServices:AuthService:BaseUrl"] ?? "http://authservice:5133";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Product Service
builder.Services.AddHttpClient<IProductService, ProductService>(client =>
{
    var baseUrl = builder.Configuration["ExternalServices:ProductService:BaseUrl"] ?? "http://productservice:5137";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Wallet Service Client
builder.Services.AddHttpClient<WalletServiceClient>(client =>
{
    var walletServiceBaseUrl = builder.Configuration["ExternalServices:WalletService:BaseUrl"] ?? "http://walletservice:5150";
    client.BaseAddress = new Uri(walletServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<OrderService.Infrastructure.ExternalServices.EscrowServiceClient>((sp, client) =>
{
     var config = sp.GetRequiredService<IConfiguration>();
     var baseUrl = config["ExternalServices:EscrowService:BaseUrl"] ?? "http://escrowservice:5141";
     client.BaseAddress = new Uri(baseUrl);
     client.Timeout = TimeSpan.FromSeconds(30);
});

// ==============================
// 🍃 MongoDB Configuration
// ==============================
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = builder.Configuration.GetSection("MongoSettings").Get<MongoSettings>();
    return new MongoClient(settings?.ConnectionString);
});

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var settings = builder.Configuration.GetSection("MongoSettings").Get<MongoSettings>();
    return client.GetDatabase(settings?.DatabaseName);
});

// ==============================
// 🩺 Health Checks
// ==============================
builder.Services.AddHealthChecks();

var app = builder.Build();

// ==============================
// 🚀 Middleware Pipeline
// ==============================
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.MapGet("/health/simple", () =>
    Results.Json(new
    {
        status = "ok",
        service = "OrderService",
        timestamp = DateTime.UtcNow
    })
);

app.Run();
