using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Services;
using ProductService.Application.DTOs;
using ProductService.Domain.Entities;

namespace ProductService.Web.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        private readonly ProductAppService _service;

        public ProductController(ProductAppService service)
        {
            _service = service;
        }

        // Lấy tất cả product
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _service.GetAll();
            return Ok(products);
        }

        // Lấy product theo Id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var product = await _service.GetById(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        // Tạo product
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductDto dto)
        {
            // Lấy thông tin user từ Gateway
            var ownerId = Request.Headers["X-User-Id"].FirstOrDefault() ?? "unknown";

            var product = new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                Attributes = dto.Attributes,
                OwnerId = ownerId
            };

            await _service.Create(product);
            return Ok(product);
        }

        // Cập nhật product
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ProductDto dto)
        {
            var product = await _service.GetById(id);
            if (product == null) return NotFound();

            // Có thể kiểm tra ownerId để chỉ cho user tạo product sửa
            var ownerId = Request.Headers["X-User-Id"].FirstOrDefault();
            if (ownerId != null && product.OwnerId != ownerId)
                return Forbid("You are not the owner of this product.");

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Attributes = dto.Attributes;

            await _service.Update(product);
            return Ok(product);
        }

        // Xóa product
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var product = await _service.GetById(id);
            if (product == null) return NotFound();

            var ownerId = Request.Headers["X-User-Id"].FirstOrDefault();
            if (ownerId != null && product.OwnerId != ownerId)
                return Forbid("You are not the owner of this product.");

            await _service.Delete(id);
            return Ok(new { success = true });
        }
    }
}
