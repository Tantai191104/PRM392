namespace AiService.Models
{
    public class AddTrainingRequest
    {
        public BatteryFeaturesDto BatteryFeatures { get; set; } = new();
        public string ActualStatus { get; set; } = string.Empty; // Good/Fair/Poor
        public float ActualPrice { get; set; } // Actual price user entered
    }
}
