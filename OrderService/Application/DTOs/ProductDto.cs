namespace OrderService.Application.DTOs
{
    public class ProductDto
    {
        public string Id { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Voltage { get; set; } = string.Empty;
        public int CycleCount { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Warranty { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal Price { get; set; }
        public List<string> Images { get; set; } = new();
        public string Description { get; set; } = string.Empty;
    }
}
