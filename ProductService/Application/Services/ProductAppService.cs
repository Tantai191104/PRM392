using ProductService.Domain.Entities;
using ProductService.Infrastructure.Repositories;

namespace ProductService.Application.Services
{
    public class ProductAppService
    {
        private readonly ProductRepository _repo;

        public ProductAppService(ProductRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Product>> GetByOwnerId(string ownerId) => await _repo.GetByOwnerIdAsync(ownerId);

        public async Task<Product> Create(Product product)
        {
            await _repo.CreateAsync(product);
            return product;
        }

        public async Task<List<Product>> GetAll() => await _repo.GetAllAsync();
        public async Task<Product?> GetById(string id) => await _repo.GetByIdAsync(id);
        public async Task Update(Product product)
        {
            await _repo.UpdateAsync(product);
        }
        public async Task Delete(string id) => await _repo.DeleteAsync(id);

        public async Task<(List<Product>, int)> GetFilteredProducts(
            string? type, string? status, string? brand, string? voltage, int? cycleCount,
            string? location, string? warranty, int page, int pageSize)
        {
            var products = await _repo.GetAllAsync();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(type))
                products = products.Where(p => p.Type == type).ToList();
            if (!string.IsNullOrWhiteSpace(status))
                products = products.Where(p => p.Status == status).ToList();
            if (!string.IsNullOrWhiteSpace(brand))
                products = products.Where(p => p.Brand == brand).ToList();
            if (!string.IsNullOrWhiteSpace(voltage))
                products = products.Where(p => p.Voltage == voltage).ToList();
            if (cycleCount.HasValue)
                products = products.Where(p => p.CycleCount == cycleCount.Value).ToList();
            if (!string.IsNullOrWhiteSpace(location))
                products = products.Where(p => p.Location == location).ToList();
            if (!string.IsNullOrWhiteSpace(warranty))
                products = products.Where(p => p.Warranty == warranty).ToList();

            // Pagination
            var total = products.Count;
            var items = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return (items, total);
        }
    }
}
