using System.Text.Json;

namespace ProductService.Application.DTOs
{
    public class ProductDto
    {
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        // Use JsonElement so System.Text.Json binds dynamic values predictably
        public Dictionary<string, JsonElement> Attributes { get; set; } = new();
    }

        // Owner information returned from AuthService
        public class OwnerDto
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }

        // Product response enriched with owner info
        public class ProductWithOwnerDto
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public Dictionary<string, object?> Attributes { get; set; } = new Dictionary<string, object?>();
            public OwnerDto Owner { get; set; } = new OwnerDto();
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }
}
