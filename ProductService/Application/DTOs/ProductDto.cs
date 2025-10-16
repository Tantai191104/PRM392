using System.Text.Json;

namespace ProductService.Application.DTOs
{
    public class ProductDto
    {
        public string Brand { get; set; } = string.Empty;
        public string Voltage { get; set; } = string.Empty;
        public int CycleCount { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Warranty { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending"; // Pending, Published, InTransaction, Sold, Expired, Rejected
        public string Name { get; set; } = null!;
        public string Type { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal Price { get; set; }
        public List<string> Images { get; set; } = new();
        public string Description { get; set; } = string.Empty;
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
        public string Brand { get; set; } = string.Empty;
        public string Voltage { get; set; } = string.Empty;
        public int CycleCount { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Warranty { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending"; // Pending, Published, InTransaction, Sold, Expired, Rejected
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal Price { get; set; }
        public List<string> Images { get; set; } = new List<string>();
        public string Description { get; set; } = string.Empty;
        public OwnerDto Owner { get; set; } = new OwnerDto();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
