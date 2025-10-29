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

        public async Task CreateAsync(Product product) =>
            await _collection.InsertOneAsync(product);

        public async Task<List<Product>> GetAllAsync() =>
            await _collection.Find(_ => true).ToListAsync();

        public async Task<Product?> GetByIdAsync(string id) =>
            await _collection.Find(p => p.Id == id).FirstOrDefaultAsync();

        public async Task UpdateAsync(Product product) =>
            await _collection.ReplaceOneAsync(p => p.Id == product.Id, product);

        public async Task DeleteAsync(string id) =>
            await _collection.DeleteOneAsync(p => p.Id == id);

        public async Task<List<Product>> GetByOwnerIdAsync(string ownerId) =>
            await _collection.Find(p => p.OwnerId == ownerId).ToListAsync();

        // ✅ Hiệu năng cao: lọc và phân trang ngay trong MongoDB
        public async Task<(List<Product>, int)> GetFilteredAsync(
            string? type, string? status, string? brand, string? voltage, int? cycleCount,
            string? location, string? warranty, int page, int pageSize)
        {
            var filterBuilder = Builders<Product>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrWhiteSpace(type))
                filter &= filterBuilder.Eq(p => p.Type, type);
            if (!string.IsNullOrWhiteSpace(status))
                filter &= filterBuilder.Eq(p => p.Status, status);
            if (!string.IsNullOrWhiteSpace(brand))
                filter &= filterBuilder.Eq(p => p.Brand, brand);
            if (!string.IsNullOrWhiteSpace(voltage))
                filter &= filterBuilder.Eq(p => p.Voltage, voltage);
            if (cycleCount.HasValue)
                filter &= filterBuilder.Eq(p => p.CycleCount, cycleCount.Value);
            if (!string.IsNullOrWhiteSpace(location))
                filter &= filterBuilder.Eq(p => p.Location, location);
            if (!string.IsNullOrWhiteSpace(warranty))
                filter &= filterBuilder.Eq(p => p.Warranty, warranty);

            var total = (int)await _collection.CountDocumentsAsync(filter);
            var items = await _collection.Find(filter)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .SortByDescending(p => p.CreatedAt) // ✅ ưu tiên mới nhất
                .ToListAsync();

            return (items, total);
        }
    }
}
