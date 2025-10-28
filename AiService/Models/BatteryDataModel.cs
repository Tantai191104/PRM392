using Microsoft.ML.Data;

namespace AiService.Models
{
    // Model for Status Classification Training Data
    public class StatusTrainingData
    {
        [LoadColumn(0)] public float RemainingCapacityPercent { get; set; }
        [LoadColumn(1)] public float Voltage { get; set; }
        [LoadColumn(2)] public float ChargeCycles { get; set; }
        [LoadColumn(3)] public float AgeMonths { get; set; }
        [LoadColumn(4)] public float TemperatureC { get; set; }
        [LoadColumn(5)] public string Brand { get; set; } = string.Empty;
        [LoadColumn(6)] public float CapacityMah { get; set; }
        [LoadColumn(7)] public float PhysicalConditionScore { get; set; }
        [LoadColumn(8)] public string Status { get; set; } = string.Empty; // Good/Fair/Poor
    }

    // Model for Price Regression Training Data
    public class PriceTrainingData
    {
        [LoadColumn(0)] public float AgeMonths { get; set; }
        [LoadColumn(1)] public string Brand { get; set; } = string.Empty;
        [LoadColumn(2)] public float CapacityAh { get; set; }
        [LoadColumn(3)] public float PhysicalConditionScore { get; set; }
        [LoadColumn(4)] public string Status { get; set; } = string.Empty;
        [LoadColumn(5)] public float Price { get; set; }
    }

    // Status Classification Prediction Output
    public class StatusPrediction
    {
        [ColumnName("PredictedLabel")] 
        public string Status { get; set; } = string.Empty;
        
        public float[] Score { get; set; } = Array.Empty<float>();
    }

    // Price Regression Prediction Output
    public class PricePrediction
    {
        [ColumnName("Score")] 
        public float Price { get; set; }
    }

    // Legacy model for backward compatibility (if needed)
    public class BatteryDataModel
    {
        [LoadColumn(0)] public float Embedded1 { get; set; }
        [LoadColumn(1)] public float Embedded2 { get; set; }
        [LoadColumn(2)] public float Embedded3 { get; set; }
        [LoadColumn(3)] public string BatteryStatus { get; set; } = string.Empty;
        [LoadColumn(4)] public float Price { get; set; }
    }

    public class BatteryPrediction
    {
        [ColumnName("PredictedLabel")] public string BatteryStatus { get; set; } = string.Empty;
        public float Score { get; set; }
        public float Price { get; set; }
    }
}
