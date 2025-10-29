using AuthService.Application.Services;
using AuthService.Infrastructure.Repositories;
using MongoDB.Driver;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===== MongoDB =====
var mongoSettings = builder.Configuration.GetSection("MongoSettings");
var client = new MongoClient(mongoSettings["ConnectionString"]);
string dbName = mongoSettings["DatabaseName"]!;

// ===== DI =====
builder.Services.AddSingleton(new UserRepository(client, dbName));
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<AuthAppService>();

// ===== Controllers + Swagger =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AuthService API",
        Version = "v1",
        Description = "API for authentication and authorization"
    });

    // Thêm JWT Authorization vào Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token."
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

// ✅ ===== CORS phải được đăng ký trước khi Build() =====
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5000", "http://127.0.0.1:5000") // Gateway UI
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ✅ Bây giờ mới Build app
var app = builder.Build();

// ===== Middleware Pipeline =====
app.UseCors(); // Cho phép Gateway gọi sang
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthService API V1");
    c.RoutePrefix = string.Empty; // Swagger tại "/"
});

// app.UseHttpsRedirection(); // ❌ Bỏ khi chạy Docker
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Health endpoint for orchestration / load balancers
app.MapGet("/health", () => Results.Json(new { status = "ok", service = "AuthService" }));
app.Run();
