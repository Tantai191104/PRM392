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
            [FromQuery] string? brand,
            [FromQuery] string? voltage,
            [FromQuery] int? cycleCount,
            [FromQuery] string? location,
            [FromQuery] string? warranty,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var (items, total) = await _service.GetFilteredProducts(
                type, "Published", brand, voltage, cycleCount, location, warranty, page, pageSize);

            var client = _httpFactory.CreateClient("auth");
            var enrichedItems = new List<ProductWithOwnerDto>();
            foreach (var product in items)
            {
                var owner = await GetOwnerAsync(client, product.OwnerId);
                enrichedItems.Add(new ProductWithOwnerDto
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
                    Owner = owner ?? new OwnerDto { FullName = null, Email = null, Phone = null },
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                });
            }
            return Ok(new { total, page, pageSize, items = enrichedItems });
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
                OwnerId = product.OwnerId, // Thêm OwnerId
                Owner = owner ?? new OwnerDto { FullName = null, Email = null, Phone = null },
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

            // Đơn giản hóa: chỉ gọi API AI prompt lấy đánh giá và giá gợi ý
            PriceSuggestionResult? aiSuggestion = null;
            try
            {
                var aiClient = _httpFactory.CreateClient("ai");
                // Gửi prompt đơn giản, chỉ truyền các trường cơ bản
                var promptRequest = new
                {
                    name = dto.Name,
                    type = dto.Type,
                    brand = dto.Brand,
                    year = dto.Year,
                    condition = dto.Condition,
                    price = dto.Price
                };
                using var resp = await aiClient.PostAsJsonAsync("/ai/api/BatteryPrediction/prompt", promptRequest);
                if (resp.IsSuccessStatusCode)
                {
                    aiSuggestion = await resp.Content.ReadFromJsonAsync<PriceSuggestionResult>();
                    // Nếu có đánh giá, lưu vào product
                    if (aiSuggestion != null)
                    {
                        product.SOH = aiSuggestion.EstimatedRemainingPercent;
                    }
                }
                else
                {
                    var err = await resp.Content.ReadAsStringAsync();
                    Console.WriteLine($"[AI Prompt] Status: {resp.StatusCode}, Body: {err}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI Prompt] Exception: {ex.Message}");
            }

            await _service.Create(product);
            return Ok(new { product, aiSuggestion });
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

        // 🔹 Approve product
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ApproveProduct(string id)
        {
            var product = await _service.GetById(id);
            if (product == null)
                return NotFound();

            product.Status = "Published";
            product.RejectionReason = null;

            await _service.Update(product);
            return Ok(product);
        }

        // 🔹 Reject product
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> RejectProduct(string id, [FromBody] RejectProductDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { success = false, message = "Reason is required for rejection" });

            var product = await _service.GetById(id);
            if (product == null)
                return NotFound();

            product.Status = "Rejected";
            product.RejectionReason = dto.Reason;

            await _service.Update(product);
            return Ok(product);
        }

        // 🔹 Lấy tất cả products với filter và pagination (Admin/Staff only)
        [HttpGet("admin")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAllForAdmin(
            [FromQuery] string? type,
            [FromQuery] string? brand,
            [FromQuery] string? voltage,
            [FromQuery] int? cycleCount,
            [FromQuery] string? location,
            [FromQuery] string? warranty,
            [FromQuery] string? status = "Pending",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var (items, total) = await _service.GetFilteredProducts(
                type, status, brand, voltage, cycleCount, location, warranty, page, pageSize);

            return Ok(new { total, page, pageSize, items });
        }

        // 🔹 Helper: convert incoming Attributes dictionary of JsonElement
        private static Dictionary<string, object?> ConvertAttributes(Dictionary<string, JsonElement> input)
        {
            var result = new Dictionary<string, object?>();
            foreach (var kvp in input)
            {
                result[kvp.Key] = kvp.Value.ValueKind switch
                {
                    JsonValueKind.String => kvp.Value.GetString(),
                    JsonValueKind.Number => kvp.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => kvp.Value.ToString()
                };
            }
            return result;
        }

        // 🔹 Helper: Map ProductDto to AI features
        private static BatteryFeaturesDto MapToAiFeatures(ProductDto dto, string? productId = null)
        {
            // Parse voltage numeric
            float voltage = 0;
            if (!string.IsNullOrWhiteSpace(dto.Voltage))
            {
                var digits = new string(dto.Voltage.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                float.TryParse(digits.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out voltage);
            }

            // Parse capacity to mAh
            int capacityMah = 0;
            if (!string.IsNullOrWhiteSpace(dto.Capacity))
            {
                var digits = new string(dto.Capacity.Where(c => char.IsDigit(c)).ToArray());
                int.TryParse(digits, out capacityMah);
                if (capacityMah == 0 && digits.Length > 0)
                {
                    int.TryParse(digits, out capacityMah);
                }
            }

            // Physical condition score from text condition
            float conditionScore = dto.Condition?.ToLowerInvariant() switch
            {
                var s when s != null && s.Contains("new") => 9.5f,
                var s when s != null && (s.Contains("like new") || s.Contains("excellent")) => 8.5f,
                var s when s != null && (s.Contains("used") || s.Contains("good")) => 6.5f,
                var s when s != null && (s.Contains("poor") || s.Contains("bad") || s.Contains("fair")) => 3.5f,
                _ => 6.0f
            };

            // Age in months from year
            int currentYear = DateTime.UtcNow.Year;
            int ageMonths = Math.Max(0, (currentYear - Math.Max(0, dto.Year)) * 12);

            // Remaining capacity heuristic from cycles + condition
            float baseRemaining = 100f - (dto.CycleCount * 0.03f);
            if (conditionScore >= 9) baseRemaining += 2;
            else if (conditionScore <= 4) baseRemaining -= 5;
            baseRemaining = Math.Clamp(baseRemaining, 10f, 100f);

            return new BatteryFeaturesDto
            {
                ProductId = productId,
                Name = dto.Name ?? string.Empty,
                Type = dto.Type ?? string.Empty,
                Capacity = dto.Capacity ?? string.Empty,
                Condition = dto.Condition ?? string.Empty,
                Year = dto.Year,
                Description = dto.Description ?? string.Empty,
                Location = dto.Location ?? string.Empty,
                Warranty = dto.Warranty ?? string.Empty,
                Brand = dto.Brand ?? string.Empty,
                Voltage = dto.Voltage ?? string.Empty,
                CycleCount = dto.CycleCount,
                RemainingCapacityPercent = baseRemaining,
                VoltageNumeric = voltage,
                AgeMonths = ageMonths,
                TemperatureC = 30f,
                CapacityMah = capacityMah,
                PhysicalConditionScore = conditionScore
            };
        }

        // 🔹 Update product status
        [HttpPut("{id}/status")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusDto dto)
        {
            var product = await _service.GetById(id);
            if (product == null)
                return NotFound();

            // Validate status
            var validStatuses = new[] { "Draft", "Pending", "Published", "InTransaction", "Sold", "Expired", "Rejected" };
            if (!validStatuses.Contains(dto.Status))
                return BadRequest(new { success = false, message = "Invalid status" });

            product.Status = dto.Status;
            product.UpdatedAt = DateTime.UtcNow;

            await _service.Update(product);
            return Ok(product);
        }

        // 🔹 Publish listing (change status from Draft to PendingReview)
        [HttpPost("{id}/publish")]
        [Authorize]
        public async Task<IActionResult> PublishListing(string id)
        {
            var product = await _service.GetById(id);
            if (product == null)
                return NotFound();

            var callerId = User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                           ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? Request.Headers["X-User-Id"].FirstOrDefault();

            if (callerId != product.OwnerId)
                return Forbid("You are not the owner of this product");

            if (product.Status != "Draft")
                return BadRequest(new { success = false, message = $"Cannot publish product with status {product.Status}" });

            product.Status = "Pending";
            product.UpdatedAt = DateTime.UtcNow;

            await _service.Update(product);
            return Ok(new { success = true, message = "Product submitted for review", product });
        }

        // 🔹 Helper: lấy thông tin chủ sở hữu sản phẩm từ dịch vụ xác thực
        private static async Task<OwnerDto?> GetOwnerAsync(HttpClient client, string ownerId)
        {
            try
            {
                var response = await client.GetAsync($"/api/users/{ownerId}");
                if (response.IsSuccessStatusCode)
                {
                    // Deserialize to a dynamic object to extract only needed fields
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    // Extract necessary fields (example: FullName, Email, Phone, AvatarUrl)
                    var owner = new OwnerDto
                    {
                        FullName = root.TryGetProperty("fullName", out var fullNameProp) ? fullNameProp.GetString() : null,
                        Email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null,
                        Phone = root.TryGetProperty("phone", out var phoneProp) ? phoneProp.GetString() : null,
                    };
                    return owner;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Owner API] Status: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Owner API] Exception: {ex.Message}");
            }
            return null;
        }
    }
}
