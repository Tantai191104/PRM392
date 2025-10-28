using AiService.Models;
using AiService.Settings;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace AiService.Infrastructure
{
    public class PredictionRepository
    {
        private readonly IMongoCollection<PredictionRecord> _collection;
        public PredictionRepository(MongoContext context, IOptions<MongoSettings> settings)
        {
            _collection = context.Database.GetCollection<PredictionRecord>(settings.Value.PredictionsCollection);
        }

        public Task InsertAsync(PredictionRecord record, CancellationToken ct = default)
            => _collection.InsertOneAsync(record, cancellationToken: ct);

        public async Task<List<PredictionRecord>> GetAllAsync(CancellationToken ct = default)
            => await _collection.Find(_ => true).ToListAsync(ct);
    }
}
