namespace AiService.Models
{
    public class BatteryFeaturesDto
    {
        // Product metadata
        public string? ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty; // e.g. "3000mAh"
        public string Condition { get; set; } = string.Empty; // e.g. "New", "Used"
        public int Year { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Warranty { get; set; } = string.Empty;
        
        // Technical features
        public string Brand { get; set; } = string.Empty;
        public string Voltage { get; set; } = string.Empty; // e.g. "3.7V"
        public int CycleCount { get; set; }
        
        // Computed/inferred features for ML
        public float RemainingCapacityPercent { get; set; }
        public float VoltageNumeric { get; set; }
        public int AgeMonths { get; set; }
        public float TemperatureC { get; set; }
        public int CapacityMah { get; set; }
        public float PhysicalConditionScore { get; set; } // 0..10
        
        // Optional labels for training
        public string? LabeledStatus { get; set; }
        public float? LabeledPrice { get; set; }
    }

    public class PriceSuggestionResult
    {
        public string Status { get; set; } = string.Empty; // Good/Fair/Poor
        public float? EstimatedRemainingPercent { get; set; }
        public double SuggestedPrice { get; set; }
        public List<SimilarBatteryInfo> SimilarBatteries { get; set; } = new List<SimilarBatteryInfo>();
    }

    public class SimilarBatteryInfo
    {
        public string ProductId { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public float Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public double SimilarityScore { get; set; }
    }
}
