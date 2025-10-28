namespace AiService.Models
{
    public class BatteryDataDto
    {
        public float[] EmbeddedData { get; set; } = Array.Empty<float>();
    }

    public class PredictionResultDto
    {
        public string BatteryStatus { get; set; } = string.Empty;
        public double Price { get; set; }
    }
}
