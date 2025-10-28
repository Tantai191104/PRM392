using AiService.Settings;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace AiService.Infrastructure
{
    public class MongoContext
    {
        public IMongoDatabase Database { get; }
        private readonly IMongoClient _client;
        private readonly MongoSettings _settings;

        public MongoContext(IMongoClient client, IOptions<MongoSettings> settings)
        {
            _client = client;
            _settings = settings.Value;
            Database = _client.GetDatabase(_settings.Database);
        }

        public IMongoCollection<T> GetCollection<T>(string? name = null)
        {
            return Database.GetCollection<T>(name ?? typeof(T).Name.ToLowerInvariant());
        }
    }
}
