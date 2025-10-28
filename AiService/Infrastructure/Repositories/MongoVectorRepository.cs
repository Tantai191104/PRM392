using AiService.Models;
using MongoDB.Driver;

namespace AiService.Infrastructure.Repositories
{
    public class MongoVectorRepository : IVectorRepository
    {
        private readonly IMongoCollection<BatteryVector> _collection;
        private readonly ILogger<MongoVectorRepository> _logger;

        public MongoVectorRepository(
            IConfiguration configuration,
            ILogger<MongoVectorRepository> logger)
        {
            _logger = logger;
            
            var connectionString = configuration["Mongo:ConnectionString"] 
                ?? throw new ArgumentNullException("Mongo:ConnectionString");
            var databaseName = configuration["Mongo:Database"] 
                ?? throw new ArgumentNullException("Mongo:Database");
            var collectionName = configuration["Mongo:VectorsCollection"] 
                ?? "battery_vectors";

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _collection = database.GetCollection<BatteryVector>(collectionName);
        }

        public async Task InsertBatteryVectorAsync(BatteryVector batteryVector)
        {
            try
            {
                await _collection.InsertOneAsync(batteryVector);
                _logger.LogInformation("Inserted battery vector for ProductId: {ProductId}", 
                    batteryVector.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting battery vector");
                throw;
            }
        }

        public async Task<List<BatteryVectorSimilarity>> SearchSimilarBatteriesAsync(
            float[] queryEmbedding, 
            int k = 5)
        {
            try
            {
                // Get all battery vectors from database
                var allBatteries = await _collection.Find(_ => true).ToListAsync();
                
                if (!allBatteries.Any())
                {
                    _logger.LogWarning("No battery vectors found in database");
                    return new List<BatteryVectorSimilarity>();
                }

                // Calculate cosine similarity for each battery
                var similarities = allBatteries
                    .Select(battery => new BatteryVectorSimilarity
                    {
                        Battery = battery,
                        SimilarityScore = CalculateCosineSimilarity(queryEmbedding, battery.Embedding)
                    })
                    .OrderByDescending(x => x.SimilarityScore)
                    .Take(k)
                    .ToList();

                _logger.LogInformation("Found {Count} similar batteries out of {Total} total batteries", 
                    similarities.Count, allBatteries.Count);

                return similarities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching similar batteries");
                throw;
            }
        }

        public async Task<List<BatteryVector>> GetAllVectorsAsync()
        {
            try
            {
                return await _collection.Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all vectors");
                throw;
            }
        }

        private float CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
            {
                throw new ArgumentException("Vectors must have the same dimension");
            }

            float dotProduct = 0;
            float magnitude1 = 0;
            float magnitude2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = (float)Math.Sqrt(magnitude1);
            magnitude2 = (float)Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
            {
                return 0;
            }

            return dotProduct / (magnitude1 * magnitude2);
        }
    }
}
