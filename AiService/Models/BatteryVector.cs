using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AiService.Models
{
    public class BatteryVector
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string ProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        
        // Battery specifications
        public float CapacityAh { get; set; }
        public float VoltageNumeric { get; set; }
        public int CycleCount { get; set; }
        public int AgeMonths { get; set; }
        public float PhysicalConditionScore { get; set; }
        public float RemainingCapacityPercent { get; set; }
        
        // Actual values (ground truth)
        public string ActualStatus { get; set; } = string.Empty; // Good/Fair/Poor
        public float ActualPrice { get; set; }
        
        // Hugging Face embedding vector (sentence-transformers/all-MiniLM-L6-v2: 384 dimensions)
        [BsonElement("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
        
        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Helper to create text for embedding
        public string ToEmbeddingText()
        {
            return $"Battery: {Brand} {Name}, " +
                   $"Capacity: {CapacityAh}Ah, " +
                   $"Voltage: {VoltageNumeric}V, " +
                   $"Cycles: {CycleCount}, " +
                   $"Age: {AgeMonths} months, " +
                   $"Condition: {Condition}, " +
                   $"Physical Score: {PhysicalConditionScore}, " +
                   $"Remaining: {RemainingCapacityPercent}%";
        }
    }
}
