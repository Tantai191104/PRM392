using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System.Text;
using System;
using WalletService.Application.Services;
using WalletService.Infrastructure.Repositories;
using WalletService.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

// -------------------------
// Add services
// -------------------------

builder.Services.AddControllers();

// Add Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
        if (jwtSettings != null && !string.IsNullOrEmpty(jwtSettings.SecretKey))
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
        }
    });

// Add Authorization DI
builder.Services.AddAuthorization();

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WalletService API", Version = "v1" });

    // Add JWT bearer support in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token like: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// -------------------------
// MongoDB
// -------------------------

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IConfiguration>().GetSection("MongoDbSettings").Get<MongoDbSettings>();
    return new MongoClient(settings?.ConnectionString ?? throw new InvalidOperationException("Mongo ConnectionString is missing"));
});

builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var settings = sp.GetRequiredService<IConfiguration>().GetSection("MongoDbSettings").Get<MongoDbSettings>();
    return client.GetDatabase(settings?.DatabaseName ?? throw new InvalidOperationException("Mongo DatabaseName is missing"));
});

// -------------------------
// DI services
// -------------------------

builder.Services.AddHttpClient();
builder.Services.AddScoped<WalletRepository>();
builder.Services.AddScoped<TransactionRepository>();
builder.Services.AddScoped<WalletAppService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<ZaloPayService>();

builder.Services.AddHealthChecks();

// -------------------------
// Build & middleware
// -------------------------

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WalletService API v1"));

app.UseHttpsRedirection();

// Enable Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization(); // Áp dụng Authorize cho tất cả controller
app.MapHealthChecks("/health");

app.Run();
