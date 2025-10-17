using ProductService.Infrastructure.Repositories;
using ProductService.Application.Services;
using MongoDB.Driver;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load environment-specific appsettings
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

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
    // Add JWT Bearer support in Swagger UI so we can use the Authorize button
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}' - you can copy the token from AuthService login response."
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

// ===== JWT Authentication (validate tokens issued by AuthService) =====
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

        // Add event handlers to help debug token validation issues at runtime
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                try
                {
                    var claims = ctx.Principal?.Claims.Select(c => new KeyValuePair<string, string>(c.Type, c.Value)).ToList()
                                 ?? new List<KeyValuePair<string, string>>();
                    Console.WriteLine("ProductService: Token validated. Claims:");
                    foreach (var c in claims)
                    {
                        Console.WriteLine($"  {c.Key} => {c.Value}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ProductService: OnTokenValidated error: {ex.Message}");
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"ProductService: Token validation failed: {ctx.Exception?.Message}");
                return Task.CompletedTask;
            }
        };
    });

// Authorization
builder.Services.AddAuthorization();

// Register an HttpClient to call AuthService for user info
builder.Services.AddHttpClient("auth", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Downstream:AuthBaseUrl") ?? "http://authservice:5133");
});

// ✅ ===== CORS phải đăng ký trước khi Build =====
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5000", "http://127.0.0.1:5000") // Gateway
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ✅ Bây giờ mới Build
var app = builder.Build();

// ===== Middleware =====
app.UseCors(); // Cho phép CORS
app.UseAuthentication();
app.UseAuthorization();
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
