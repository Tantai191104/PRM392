using EscrowService.Domain.Entities;
using MongoDB.Driver;

namespace EscrowService.Infrastructure.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment?> GetByIdAsync(string id);
        Task<List<Payment>> GetByEscrowIdAsync(string escrowId);
        Task<Payment?> GetByIntentIdAsync(string provider, string intentId);
    }

    public class PaymentRepository : IPaymentRepository
    {
        private readonly IMongoCollection<Payment> _collection;

        public PaymentRepository(IMongoClient client, string databaseName)
        {
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<Payment>("payments");

            // Create indexes
            var indexKeys1 = Builders<Payment>.IndexKeys.Ascending(p => p.EscrowId);
            var indexModel1 = new CreateIndexModel<Payment>(indexKeys1);

            var indexKeys2 = Builders<Payment>.IndexKeys
                .Ascending(p => p.Provider)
                .Ascending(p => p.IntentId);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel2 = new CreateIndexModel<Payment>(indexKeys2, indexOptions);

            _collection.Indexes.CreateManyAsync(new[] { indexModel1, indexModel2 });
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            payment.CreatedAt = DateTime.UtcNow;
            await _collection.InsertOneAsync(payment);
            return payment;
        }

        public async Task<Payment?> GetByIdAsync(string id)
        {
            return await _collection.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<Payment>> GetByEscrowIdAsync(string escrowId)
        {
            return await _collection.Find(p => p.EscrowId == escrowId).ToListAsync();
        }

        public async Task<Payment?> GetByIntentIdAsync(string provider, string intentId)
        {
            return await _collection.Find(p => p.Provider == provider && p.IntentId == intentId).FirstOrDefaultAsync();
        }
    }
}

