using AiService.Models;
using AiService.Settings;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace AiService.Infrastructure
{
    public class TrainingRepository
    {
        private readonly IMongoCollection<TrainingRecord> _collection;
        public TrainingRepository(MongoContext context, IOptions<MongoSettings> settings)
        {
            _collection = context.Database.GetCollection<TrainingRecord>(settings.Value.TrainingCollection);
        }

        public Task InsertAsync(TrainingRecord record, CancellationToken ct = default)
            => _collection.InsertOneAsync(record, cancellationToken: ct);
        public async Task<List<TrainingRecord>> GetAllAsync(CancellationToken ct = default)
            => await _collection.Find(_ => true).ToListAsync(ct);
    }
}
