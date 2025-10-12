using MongoDB.Driver;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Repositories
{
    public class ProductRepository
    {
        private readonly IMongoCollection<Product> _collection;

        public ProductRepository(IMongoClient client, string databaseName)
        {
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<Product>("Products");
        }

        public async Task CreateAsync(Product product) => await _collection.InsertOneAsync(product);
        public async Task<List<Product>> GetAllAsync() => await _collection.Find(_ => true).ToListAsync();
        public async Task<Product?> GetByIdAsync(string id) => await _collection.Find(p => p.Id == id).FirstOrDefaultAsync();
        public async Task UpdateAsync(Product product) => await _collection.ReplaceOneAsync(p => p.Id == product.Id, product);
        public async Task DeleteAsync(string id) => await _collection.DeleteOneAsync(p => p.Id == id);
    }
}
