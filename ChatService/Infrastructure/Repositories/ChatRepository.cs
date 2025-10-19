using ChatService.Domain.Entities;
using MongoDB.Driver;

namespace ChatService.Infrastructure.Repositories
{
    public interface IChatRepository
    {
        Task<Chat> CreateAsync(Chat chat);
        Task<Chat?> GetByIdAsync(string id);
        Task<Chat?> GetByListingAndBuyerAsync(string listingId, string buyerId);
        Task<List<Chat>> GetByUserIdAsync(string userId);
        Task<Chat> UpdateAsync(Chat chat);
        Task<bool> DeleteAsync(string id);
    }

    public class ChatRepository : IChatRepository
    {
        private readonly IMongoCollection<Chat> _collection;

        public ChatRepository(IMongoClient client, string databaseName)
        {
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<Chat>("chats");

            // Create indexes
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexKeys1 = Builders<Chat>.IndexKeys
                .Ascending(c => c.ListingId)
                .Ascending(c => c.BuyerId)
                .Ascending(c => c.SellerId);
            var indexModel1 = new CreateIndexModel<Chat>(indexKeys1);

            var indexKeys2 = Builders<Chat>.IndexKeys.Ascending(c => c.OrderId);
            var indexModel2 = new CreateIndexModel<Chat>(indexKeys2);

            var indexKeys3 = Builders<Chat>.IndexKeys.Descending(c => c.LastMessageAt);
            var indexModel3 = new CreateIndexModel<Chat>(indexKeys3);

            _collection.Indexes.CreateManyAsync(new[] { indexModel1, indexModel2, indexModel3 });
        }

        public async Task<Chat> CreateAsync(Chat chat)
        {
            chat.CreatedAt = DateTime.UtcNow;
            await _collection.InsertOneAsync(chat);
            return chat;
        }

        public async Task<Chat?> GetByIdAsync(string id)
        {
            return await _collection.Find(c => c.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Chat?> GetByListingAndBuyerAsync(string listingId, string buyerId)
        {
            return await _collection.Find(c => c.ListingId == listingId && c.BuyerId == buyerId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Chat>> GetByUserIdAsync(string userId)
        {
            return await _collection.Find(c => c.BuyerId == userId || c.SellerId == userId)
                .SortByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        public async Task<Chat> UpdateAsync(Chat chat)
        {
            chat.UpdatedAt = DateTime.UtcNow;
            await _collection.ReplaceOneAsync(c => c.Id == chat.Id, chat);
            return chat;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _collection.DeleteOneAsync(c => c.Id == id);
            return result.DeletedCount > 0;
        }
    }
}

