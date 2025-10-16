using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ProductService.Application.Services;
using ProductService.Application.DTOs;
using ProductService.Domain.Entities;
using System.Text.Json;
using System.Linq;

namespace ProductService.Web.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        private readonly ProductAppService _service;
        private readonly IHttpClientFactory _httpFactory;

        public ProductController(ProductAppService service, IHttpClientFactory httpFactory)
        {
            _service = service;
            _httpFactory = httpFactory;
        }

        // 🔹 Lấy tất cả products với filter và pagination
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? type,
            [FromQuery] string? status,
            [FromQuery] string? brand,
            [FromQuery] string? voltage,
            [FromQuery] int? cycleCount,
            [FromQuery] string? location,
            [FromQuery] string? warranty,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var products = await _service.GetAll();

            // Áp dụng filter
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

            var client = _httpFactory.CreateClient("auth");
            var result = new List<ProductWithOwnerDto>();

            foreach (var p in items)
            {
                var owner = await GetOwnerAsync(client, p.OwnerId);
                result.Add(new ProductWithOwnerDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Type = p.Type,
                    Capacity = p.Capacity,
                    Condition = p.Condition,
                    Year = p.Year,
                    Price = p.Price,
                    Images = p.Images ?? new List<string>(),
                    Description = p.Description,
                    Status = p.Status,
                    Brand = p.Brand,
                    Voltage = p.Voltage,
                    CycleCount = p.CycleCount,
                    Location = p.Location,
                    Warranty = p.Warranty,
                    Owner = owner ?? new OwnerDto { Id = p.OwnerId },
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                });
            }

            return Ok(new { total, page, pageSize, items = result });
        }

        // 🔹 API lấy danh sách sản phẩm của người dùng đang đăng nhập
        [HttpGet("my-products")]
        [Authorize]
        public async Task<IActionResult> GetMyProducts()
        {
            var userId = User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                         ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? Request.Headers["X-User-Id"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var products = await _service.GetByOwnerId(userId);
            return Ok(products);
        }

        // 🔹 Lấy product theo Id
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(string id)
        {
            var product = await _service.GetById(id);
            if (product == null)
                return NotFound();

            var client = _httpFactory.CreateClient("auth");
            var owner = await GetOwnerAsync(client, product.OwnerId);

            var dto = new ProductWithOwnerDto
            {
                Id = product.Id,
                Name = product.Name,
                Type = product.Type,
                Capacity = product.Capacity,
                Condition = product.Condition,
                Year = product.Year,
                Price = product.Price,
                Images = product.Images ?? new List<string>(),
                Description = product.Description,
                Status = product.Status,
                Brand = product.Brand,
                Voltage = product.Voltage,
                CycleCount = product.CycleCount,
                Location = product.Location,
                Warranty = product.Warranty,
                Owner = owner ?? new OwnerDto { Id = product.OwnerId },
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            return Ok(dto);
        }

        // 🔹 Tạo product
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] ProductDto dto)
        {
            if (dto == null)
                return BadRequest(new { success = false, message = "Missing body" });

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { success = false, message = "Name is required" });

            if (dto.Price < 0)
                return BadRequest(new { success = false, message = "Price must be >= 0" });

            var ownerId = User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                          ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? Request.Headers["X-User-Id"].FirstOrDefault()
                          ?? "unknown";

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Type = dto.Type,
                Capacity = dto.Capacity,
                Condition = dto.Condition,
                Year = dto.Year,
                Price = dto.Price,
                Images = dto.Images ?? new List<string>(),
                Description = dto.Description,
                Status = dto.Status ?? "Pending",
                Brand = dto.Brand,
                Voltage = dto.Voltage,
                CycleCount = dto.CycleCount,
                Location = dto.Location,
                Warranty = dto.Warranty,
                OwnerId = ownerId
            };

            await _service.Create(product);
            return Ok(product);
        }

        // 🔹 Cập nhật product
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] ProductDto dto)
        {
            var product = await _service.GetById(id);
            if (product == null)
                return NotFound();

            var callerId = User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                           ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? Request.Headers["X-User-Id"].FirstOrDefault();

            if (callerId != null && product.OwnerId != callerId)
                return Forbid("You are not the owner of this product.");

            if (dto == null)
                return BadRequest(new { success = false, message = "Missing body" });

            if (!string.IsNullOrWhiteSpace(dto.Name))
                product.Name = dto.Name.Trim();

            product.Type = dto.Type;
            product.Capacity = dto.Capacity;
            product.Condition = dto.Condition;
            product.Year = dto.Year;
            if (dto.Price >= 0)
                product.Price = dto.Price;
            product.Brand = dto.Brand;
            product.Voltage = dto.Voltage;
            product.CycleCount = dto.CycleCount;
            product.Location = dto.Location;
            product.Warranty = dto.Warranty;
            product.Images = dto.Images ?? new List<string>();
            product.Description = dto.Description;
            product.Status = dto.Status ?? product.Status;

            await _service.Update(product);
            return Ok(product);
        }

        // 🔹 Xóa product
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            var product = await _service.GetById(id);
            if (product == null)
                return NotFound();

            var callerId = User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                           ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? Request.Headers["X-User-Id"].FirstOrDefault();

            if (callerId != null && product.OwnerId != callerId)
                return Forbid("You are not the owner of this product.");

            await _service.Delete(id);
            return Ok(new { success = true });
        }

        // 🔹 Helper: convert incoming Attributes dictionary of JsonElement
        private static Dictionary<string, object?> ConvertAttributes(Dictionary<string, JsonElement> input)
        {
            var result = new Dictionary<string, object?>();
            if (input == null) return result;

            foreach (var kv in input)
                result[kv.Key] = JsonElementToObject(kv.Value);

            return result;
        }

        // 🔹 Call AuthService to get owner info
        private async Task<OwnerDto?> GetOwnerAsync(HttpClient client, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                return null;

            try
            {
                var resp = await client.GetAsync($"/api/users/{ownerId}");
                if (!resp.IsSuccessStatusCode)
                    return null;

                var json = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<OwnerDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        // 🔹 Convert JsonElement → Object (cho MongoDB)
        private static object? JsonElementToObject(JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.String => je.GetString(),
                JsonValueKind.Number => je.TryGetInt64(out var l) ? l :
                                        je.TryGetDouble(out var d) ? d : je.GetDecimal(),
                JsonValueKind.True or JsonValueKind.False => je.GetBoolean(),
                JsonValueKind.Object => je.EnumerateObject().ToDictionary(
                    prop => prop.Name, prop => JsonElementToObject(prop.Value)),
                JsonValueKind.Array => je.EnumerateArray().Select(JsonElementToObject).ToList(),
                _ => null
            };
        }
    }
}
