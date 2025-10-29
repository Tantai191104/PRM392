using ChatService.Domain.Entities;
using MongoDB.Driver;

namespace ChatService.Infrastructure.Repositories
{
    public interface IMessageRepository
    {
        Task<ChatMessage> CreateAsync(ChatMessage message);
        Task<ChatMessage?> GetByIdAsync(string id);
        Task<List<ChatMessage>> GetByChatIdAsync(string chatId, int limit = 50, int skip = 0);
        Task<ChatMessage> UpdateAsync(ChatMessage message);
        Task<long> GetUnreadCountAsync(string chatId, string userId);
        Task MarkAsReadAsync(string chatId, string userId);
    }

    public class MessageRepository : IMessageRepository
    {
        private readonly IMongoCollection<ChatMessage> _collection;

        public MessageRepository(IMongoClient client, string databaseName)
        {
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<ChatMessage>("chat_messages");

            // Create indexes
            var indexKeys1 = Builders<ChatMessage>.IndexKeys.Ascending(m => m.ChatId);
            var indexModel1 = new CreateIndexModel<ChatMessage>(indexKeys1);

            var indexKeys2 = Builders<ChatMessage>.IndexKeys.Descending(m => m.CreatedAt);
            var indexModel2 = new CreateIndexModel<ChatMessage>(indexKeys2);

            _collection.Indexes.CreateManyAsync(new[] { indexModel1, indexModel2 });
        }

        public async Task<ChatMessage> CreateAsync(ChatMessage message)
        {
            message.CreatedAt = DateTime.UtcNow;
            await _collection.InsertOneAsync(message);
            return message;
        }

        public async Task<ChatMessage?> GetByIdAsync(string id)
        {
            return await _collection.Find(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<ChatMessage>> GetByChatIdAsync(string chatId, int limit = 50, int skip = 0)
        {
            return await _collection.Find(m => m.ChatId == chatId)
                .SortByDescending(m => m.CreatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();
        }

        public async Task<ChatMessage> UpdateAsync(ChatMessage message)
        {
            message.UpdatedAt = DateTime.UtcNow;
            await _collection.ReplaceOneAsync(m => m.Id == message.Id, message);
            return message;
        }

        public async Task<long> GetUnreadCountAsync(string chatId, string userId)
        {
            return await _collection.CountDocumentsAsync(
                m => m.ChatId == chatId && !m.ReadBy.Contains(userId));
        }

        public async Task MarkAsReadAsync(string chatId, string userId)
        {
            var filter = Builders<ChatMessage>.Filter.And(
                Builders<ChatMessage>.Filter.Eq(m => m.ChatId, chatId),
                Builders<ChatMessage>.Filter.Not(
                    Builders<ChatMessage>.Filter.AnyEq(m => m.ReadBy, userId)
                )
            );

            var update = Builders<ChatMessage>.Update.AddToSet(m => m.ReadBy, userId);

            await _collection.UpdateManyAsync(filter, update);
        }
    }
}

