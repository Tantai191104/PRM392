using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using WalletService.Application.Services;
using WalletService.Infrastructure.Repositories;
using WalletService.Infrastructure.VNPay;
using WalletService.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// 🌍 Add core services
// ==============================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "WalletService API",
        Version = "v1",
        Description = "API cho hệ thống ví điện tử, giao dịch và VNPay tích hợp."
    });
});

// ==============================
// ⚙️ Configuration binding
// ==============================
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
// Note: appsettings.json uses the "VNPay" section. Bind VNPaySettings from that section.
builder.Services.Configure<VNPaySettings>(builder.Configuration.GetSection("VNPay"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// ==============================
// 🍃 MongoDB setup
// ==============================
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
    return new MongoClient(settings?.ConnectionString);
});

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var settings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
    return client.GetDatabase(settings?.DatabaseName);
});

// ==============================
// 🔐 JWT Authentication (optional)
// ==============================
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (!string.IsNullOrEmpty(jwtSettings?.SecretKey))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
            };
        });
}

// ==============================
// 🧩 Dependency Injection
// ==============================
builder.Services.AddHttpClient();
builder.Services.AddScoped<WalletRepository>();
builder.Services.AddScoped<TransactionRepository>();
builder.Services.AddScoped<WalletAppService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<IVnPayService, VNPayService>();


// ==============================
// 🩺 Health checks
// ==============================
builder.Services.AddHealthChecks();

var app = builder.Build();

// ==============================
// 🚀 Middleware pipeline
// ==============================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WalletService API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/health/simple", () =>
    Results.Json(new
    {
        status = "ok",
        service = "WalletService",
        environment = app.Environment.EnvironmentName,
        timestamp = DateTime.UtcNow
    })
);

app.Run();
