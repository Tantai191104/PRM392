using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SharedKernel.Entities;

namespace EscrowService.Domain.Entities
{
    public class Webhook : BaseEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public override string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("source")]
        public required string Source { get; set; }

        [BsonElement("event_id")]
        public required string EventId { get; set; }

        [BsonElement("event_type")]
        public required string EventType { get; set; }

        [BsonElement("payload")]
        public required Dictionary<string, object> Payload { get; set; }

        [BsonElement("processed")]
        public bool Processed { get; set; } = false;

        [BsonElement("processed_at")]
        public DateTime? ProcessedAt { get; set; }
    }
}

