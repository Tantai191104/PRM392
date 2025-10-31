using EscrowService.Application.Saga;
using EscrowService.Application.Services;
using EscrowService.Infrastructure.ExternalServices;
using EscrowService.Infrastructure.Providers;
using EscrowService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Serilog;
using System.Text;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/escrowservice-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// ===== MongoDB =====
var mongoSettings = builder.Configuration.GetSection("MongoSettings");
var client = new MongoClient(mongoSettings["ConnectionString"]);
string dbName = mongoSettings["DatabaseName"]!;

// ===== DI - Repositories =====
builder.Services.AddSingleton<IEscrowRepository>(sp => new EscrowRepository(client, dbName));
builder.Services.AddSingleton<IPaymentRepository>(sp => new PaymentRepository(client, dbName));
builder.Services.AddSingleton<IWebhookRepository>(sp => new WebhookRepository(client, dbName));

// ===== DI - Infrastructure Services =====
builder.Services.AddSingleton<IPaymentProvider, MockPaymentProvider>();

// ===== DI - External Service Clients =====
// Đăng ký WalletServiceClient với HttpClient và cấu hình BaseAddress
var walletServiceBaseUrl = builder.Configuration["ExternalServices:WalletService:BaseUrl"] ?? "http://walletservice:5150";
builder.Services.AddHttpClient<EscrowService.Infrastructure.ExternalServices.WalletServiceClient>(client =>
{
    client.BaseAddress = new Uri(walletServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>(client =>
{
    var baseUrl = builder.Configuration["ExternalServices:ProductService:BaseUrl"] ?? "http://productservice:5137";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IOrderServiceClient, OrderServiceClient>(client =>
{
    var baseUrl = builder.Configuration["ExternalServices:OrderService:BaseUrl"] ?? "http://orderservice:5139";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ===== DI - Application Services =====
builder.Services.AddScoped<IEscrowAppService, EscrowAppService>();
builder.Services.AddScoped<EscrowSagaOrchestrator>();

// ===== Controllers + Swagger =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EscrowService API",
        Version = "v1",
        Description = "API for escrow management with Saga Pattern for distributed transactions"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// ===== JWT Authentication =====
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
});

builder.Services.AddAuthorization();

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

// ===== Middleware Pipeline =====
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/api/escrows/swagger/v1/swagger.json", "EscrowService API V1");
    c.RoutePrefix = "api/escrows/swagger";
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health endpoint
app.MapGet("/health", () => Results.Json(new { status = "ok", service = "EscrowService" }));

app.Run();

