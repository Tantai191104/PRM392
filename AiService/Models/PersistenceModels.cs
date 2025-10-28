using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AiService.Models
{
    public class PredictionRecord
    {
        [BsonId] public ObjectId Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public BatteryFeaturesDto Input { get; set; } = new();
        public PriceSuggestionResult Output { get; set; } = new();
    }

    public class TrainingRecord
    {
        [BsonId] public ObjectId Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public BatteryFeaturesDto Data { get; set; } = new();
    }
}
