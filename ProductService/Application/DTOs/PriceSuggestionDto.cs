namespace ProductService.Application.DTOs
{
    public class PriceSuggestionRequest
    {
        public string? Brand { get; set; }
        public int? Year { get; set; }
        public int? CycleCount { get; set; }
        public string? Capacity { get; set; }
        public string? Condition { get; set; }
        public string? Voltage { get; set; }
    }

    public class PriceSuggestionResponse
    {
    public decimal SuggestedPrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public string PriceRange { get; set; } = string.Empty;
    public List<string> Factors { get; set; } = new();
    public string Explanation { get; set; } = string.Empty;
    public double SOH { get; set; }
    }

    public class UpdateStatusDto
    {
        public required string Status { get; set; }
    }
}

