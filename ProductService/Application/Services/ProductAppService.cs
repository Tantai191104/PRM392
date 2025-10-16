using ProductService.Domain.Entities;
using ProductService.Infrastructure.Repositories; // namespace chính xác

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
    }
}
