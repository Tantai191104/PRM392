using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Services;
using ProductService.Application.DTOs;
using ProductService.Domain.Entities;
using System.Text.Json;
using System.Linq;

namespace ProductService.Web.Controllers
{
    [ApiController]
    [Route("api/products")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ProductController : ControllerBase
    {
        private readonly ProductAppService _service;
        private readonly IHttpClientFactory _httpFactory;

        public ProductController(ProductAppService service, IHttpClientFactory httpFactory)
        {
            _service = service;
            _httpFactory = httpFactory;
        }

        // Lấy tất cả product
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _service.GetAll();
            var client = _httpFactory.CreateClient("auth");
            var result = new List<ProductService.Application.DTOs.ProductWithOwnerDto>();
            foreach (var p in products)
            {
                var owner = await GetOwnerAsync(client, p.OwnerId);
                result.Add(new ProductService.Application.DTOs.ProductWithOwnerDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = (decimal)p.Price,
                    Attributes = p.Attributes ?? new Dictionary<string, object?>(),
                    Owner = owner ?? new ProductService.Application.DTOs.OwnerDto { Id = p.OwnerId },
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                });
            }
            return Ok(result);
        }

        // Lấy product theo Id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var product = await _service.GetById(id);
            if (product == null) return NotFound();
            var client = _httpFactory.CreateClient("auth");
            var owner = await GetOwnerAsync(client, product.OwnerId);
            var dto = new ProductService.Application.DTOs.ProductWithOwnerDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = (decimal)product.Price,
                Attributes = product.Attributes ?? new Dictionary<string, object?>(),
                Owner = owner ?? new ProductService.Application.DTOs.OwnerDto { Id = product.OwnerId },
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
            return Ok(dto);
        }

        // Tạo product
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductDto dto)
        {
            if (dto == null) return BadRequest(new { success = false, message = "Missing body" });
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { success = false, message = "Name is required" });
            if (dto.Price < 0) return BadRequest(new { success = false, message = "Price must be >= 0" });

            // Prefer authenticated user id (sub claim) if present. The controller is [Authorize],
            // so User should be populated when a valid JWT is provided.
            // Resolve user id from common claim types: 'sub' (JWT), or NameIdentifier URI used by ASP.NET mapping.
            var ownerId = User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                          ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? Request.Headers["X-User-Id"].FirstOrDefault()
                          ?? "unknown";

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Price = dto.Price,
                Attributes = ConvertAttributes(dto.Attributes),
                OwnerId = ownerId
            };

            await _service.Create(product);
            return Ok(product);
        }

        // Simple endpoint to show auth status and claims (for debugging)
        [HttpGet("debug/me")]
        public IActionResult DebugMe()
        {
            var isAuth = User?.Identity?.IsAuthenticated ?? false;
            var claims = User?.Claims.Select(c => new KeyValuePair<string, string>(c.Type, c.Value)).ToList()
                         ?? new List<KeyValuePair<string, string>>();
            return Ok(new { authenticated = isAuth, claims });
        }

        // Cập nhật product
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ProductDto dto)
        {
            var product = await _service.GetById(id);
            if (product == null) return NotFound();

            // Có thể kiểm tra ownerId để chỉ cho user tạo product sửa
            var callerId = User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                           ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? Request.Headers["X-User-Id"].FirstOrDefault();
            if (callerId != null && product.OwnerId != callerId)
                return Forbid("You are not the owner of this product.");

            if (dto == null) return BadRequest(new { success = false, message = "Missing body" });
            if (!string.IsNullOrWhiteSpace(dto.Name)) product.Name = dto.Name.Trim();
            if (dto.Price >= 0) product.Price = dto.Price;
            product.Attributes = ConvertAttributes(dto.Attributes);

            await _service.Update(product);
            return Ok(product);
        }

        // Xóa product
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var product = await _service.GetById(id);
            if (product == null) return NotFound();

            var callerId2 = User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                           ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? Request.Headers["X-User-Id"].FirstOrDefault();
            if (callerId2 != null && product.OwnerId != callerId2)
                return Forbid("You are not the owner of this product.");

            await _service.Delete(id);
            return Ok(new { success = true });
        }

        // Helper: convert incoming Attributes dictionary of JsonElement
        // into a Dictionary<string, object> with CLR values so MongoDB can serialize it.
        private static Dictionary<string, object?> ConvertAttributes(Dictionary<string, JsonElement> input)
        {
            var result = new Dictionary<string, object?>();
            if (input == null) return new Dictionary<string, object?>();

            foreach (var kv in input)
            {
                result[kv.Key] = JsonElementToObject(kv.Value);
            }

            return result;
        }

        // Call AuthService to get owner info. Returns null if not available.
        private async Task<ProductService.Application.DTOs.OwnerDto?> GetOwnerAsync(HttpClient client, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return null;
            try
            {
                var resp = await client.GetAsync($"/api/users/{ownerId}");
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadAsStringAsync();
                var obj = System.Text.Json.JsonSerializer.Deserialize<ProductService.Application.DTOs.OwnerDto>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return obj;
            }
            catch
            {
                return null;
            }
        }

        private static object? JsonElementToObject(JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.String:
                    return je.GetString();
                case JsonValueKind.Number:
                    if (je.TryGetInt64(out var l)) return l;
                    if (je.TryGetDouble(out var d)) return d;
                    return je.GetDecimal();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return je.GetBoolean();
                case JsonValueKind.Object:
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in je.EnumerateObject())
                    {
                        dict[prop.Name] = JsonElementToObject(prop.Value);
                    }
                    return dict;
                }
                case JsonValueKind.Array:
                {
                    var list = new List<object?>();
                    foreach (var item in je.EnumerateArray()) list.Add(JsonElementToObject(item));
                    return list;
                }
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                default:
                    return null;
            }
        }
    }
}
