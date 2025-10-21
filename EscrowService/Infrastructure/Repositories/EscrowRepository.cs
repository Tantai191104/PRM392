using EscrowService.Domain.Entities;
using MongoDB.Driver;

namespace EscrowService.Infrastructure.Repositories
{
    public interface IEscrowRepository
    {
        Task<Escrow> CreateAsync(Escrow escrow);
        Task<Escrow?> GetByIdAsync(string id);
        Task<Escrow?> GetByOrderIdAsync(string orderId);
        Task<List<Escrow>> GetByBuyerIdAsync(string buyerId);
        Task<List<Escrow>> GetBySellerIdAsync(string sellerId);
        Task<Escrow> UpdateAsync(Escrow escrow);
        Task<List<Escrow>> GetAllAsync();
    }

    public class EscrowRepository : IEscrowRepository
    {
        private readonly IMongoCollection<Escrow> _collection;

        public EscrowRepository(IMongoClient client, string databaseName)
        {
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<Escrow>("escrows");

            // Create indexes
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexKeys1 = Builders<Escrow>.IndexKeys.Ascending(e => e.OrderId);
            var indexModel1 = new CreateIndexModel<Escrow>(indexKeys1);

            var indexKeys2 = Builders<Escrow>.IndexKeys.Ascending(e => e.ListingId);
            var indexModel2 = new CreateIndexModel<Escrow>(indexKeys2);

            var indexKeys3 = Builders<Escrow>.IndexKeys.Ascending(e => e.BuyerId);
            var indexModel3 = new CreateIndexModel<Escrow>(indexKeys3);

            var indexKeys4 = Builders<Escrow>.IndexKeys.Ascending(e => e.SellerId);
            var indexModel4 = new CreateIndexModel<Escrow>(indexKeys4);

            var indexKeys5 = Builders<Escrow>.IndexKeys.Ascending(e => e.Status);
            var indexModel5 = new CreateIndexModel<Escrow>(indexKeys5);

            _collection.Indexes.CreateManyAsync(new[] { indexModel1, indexModel2, indexModel3, indexModel4, indexModel5 });
        }

        public async Task<Escrow> CreateAsync(Escrow escrow)
        {
            escrow.CreatedAt = DateTime.UtcNow;
            await _collection.InsertOneAsync(escrow);
            return escrow;
        }

        public async Task<Escrow?> GetByIdAsync(string id)
        {
            return await _collection.Find(e => e.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Escrow?> GetByOrderIdAsync(string orderId)
        {
            return await _collection.Find(e => e.OrderId == orderId).FirstOrDefaultAsync();
        }

        public async Task<List<Escrow>> GetByBuyerIdAsync(string buyerId)
        {
            return await _collection.Find(e => e.BuyerId == buyerId).ToListAsync();
        }

        public async Task<List<Escrow>> GetBySellerIdAsync(string sellerId)
        {
            return await _collection.Find(e => e.SellerId == sellerId).ToListAsync();
        }

        public async Task<Escrow> UpdateAsync(Escrow escrow)
        {
            escrow.UpdatedAt = DateTime.UtcNow;
            await _collection.ReplaceOneAsync(e => e.Id == escrow.Id, escrow);
            return escrow;
        }

        public async Task<List<Escrow>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }
    }
}

