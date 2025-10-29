using EscrowService.Domain.Entities;
using MongoDB.Driver;

namespace EscrowService.Infrastructure.Repositories
{
    public interface IWebhookRepository
    {
        Task<Webhook> CreateAsync(Webhook webhook);
        Task<Webhook?> GetByEventIdAsync(string source, string eventId);
        Task<List<Webhook>> GetUnprocessedAsync(int limit = 100);
        Task MarkAsProcessedAsync(string id);
    }

    public class WebhookRepository : IWebhookRepository
    {
        private readonly IMongoCollection<Webhook> _collection;

        public WebhookRepository(IMongoClient client, string databaseName)
        {
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<Webhook>("webhooks");

            // Create unique index on source + event_id
            var indexKeys = Builders<Webhook>.IndexKeys
                .Ascending(w => w.Source)
                .Ascending(w => w.EventId);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<Webhook>(indexKeys, indexOptions);

            var processedIndex = Builders<Webhook>.IndexKeys.Ascending(w => w.Processed);
            var processedModel = new CreateIndexModel<Webhook>(processedIndex);

            _collection.Indexes.CreateManyAsync(new[] { indexModel, processedModel });
        }

        public async Task<Webhook> CreateAsync(Webhook webhook)
        {
            webhook.CreatedAt = DateTime.UtcNow;
            await _collection.InsertOneAsync(webhook);
            return webhook;
        }

        public async Task<Webhook?> GetByEventIdAsync(string source, string eventId)
        {
            return await _collection.Find(w => w.Source == source && w.EventId == eventId).FirstOrDefaultAsync();
        }

        public async Task<List<Webhook>> GetUnprocessedAsync(int limit = 100)
        {
            return await _collection.Find(w => !w.Processed)
                .Limit(limit)
                .ToListAsync();
        }

        public async Task MarkAsProcessedAsync(string id)
        {
            var update = Builders<Webhook>.Update
                .Set(w => w.Processed, true)
                .Set(w => w.ProcessedAt, DateTime.UtcNow);

            await _collection.UpdateOneAsync(w => w.Id == id, update);
        }
    }
}

