using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using AiService.Services;
using AiService.Settings;
using AiService.Infrastructure;
using AiService.Application.Services;
using AiService.Infrastructure.Repositories;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Mongo settings & client
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var cfg = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoSettings>>().Value;
    return new MongoClient(cfg.ConnectionString);
});
builder.Services.AddSingleton<MongoContext>();

// Local Embedding Service (sentence-transformers running locally - NO external API calls)
builder.Services.AddHttpClient<IEmbeddingService, LocalEmbeddingService>();

// Vector Repository for similarity search
builder.Services.AddSingleton<IVectorRepository, MongoVectorRepository>();

// App services
builder.Services.AddSingleton<BatteryPredictionService>();
builder.Services.AddSingleton<PredictionRepository>();
builder.Services.AddSingleton<TrainingRepository>();

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Json(new { status = "ok", service = "AiService" }));

app.Run();
