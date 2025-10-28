namespace ProductService.Application.DTOs
{
    // DTO khớp với AiService.Models.BatteryFeaturesDto
    public class BatteryFeaturesDto
    {
        // Product metadata
        public string? ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Warranty { get; set; } = string.Empty;
        
        // Technical features
        public string Brand { get; set; } = string.Empty;
        public string Voltage { get; set; } = string.Empty;
        public int CycleCount { get; set; }
        
        // Computed features
        public float RemainingCapacityPercent { get; set; }
        public float VoltageNumeric { get; set; }
        public int AgeMonths { get; set; }
        public float TemperatureC { get; set; }
        public int CapacityMah { get; set; }
        public float PhysicalConditionScore { get; set; }
        
        // Optional labels
        public string? LabeledStatus { get; set; }
        public float? LabeledPrice { get; set; }
    }

    // Response khớp với AiService.Models.PriceSuggestionResult
    public class PriceSuggestionResult
    {
        public string Status { get; set; } = string.Empty;
        public float? EstimatedRemainingPercent { get; set; }
        public double SuggestedPrice { get; set; }
    }
}
