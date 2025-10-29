using MongoDB.Driver;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace OrderService.Infrastructure.Repositories
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetAllAsync();
        Task<Order?> GetByIdAsync(string id);
        Task<IEnumerable<Order>> GetByUserIdAsync(string userId);
        Task<Order> CreateAsync(Order order);
        Task<Order> UpdateAsync(Order order);
        Task<bool> DeleteAsync(string id);
    }

    public class OrderRepository : IOrderRepository
    {
        private readonly IMongoCollection<Order> _orders;

        public OrderRepository(IOptions<MongoSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _orders = database.GetCollection<Order>(mongoSettings.Value.OrdersCollectionName);
        }

        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            return await _orders.Find(_ => true).ToListAsync();
        }

        public async Task<Order?> GetByIdAsync(string id)
        {
            return await _orders.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId)
        {
            return await _orders.Find(x => x.Buyer.Id == userId || x.Seller.Id == userId).ToListAsync();
        }

        public async Task<Order> CreateAsync(Order order)
        {
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            await _orders.InsertOneAsync(order);
            return order;
        }

        public async Task<Order> UpdateAsync(Order order)
        {
            order.UpdatedAt = DateTime.UtcNow;
            await _orders.ReplaceOneAsync(x => x.Id == order.Id, order);
            return order;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _orders.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0;
        }
    }
}