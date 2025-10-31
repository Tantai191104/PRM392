using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SharedKernel.Entities;

namespace EscrowService.Domain.Entities
{
    public class Payment : BaseEntity
    {
        [BsonElement("escrow_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string EscrowId { get; set; }

        [BsonElement("provider")]
        public required string Provider { get; set; }

        [BsonElement("intent_id")]
        public required string IntentId { get; set; }

        [BsonElement("action")]
        [BsonRepresentation(BsonType.String)]
        public PaymentAction Action { get; set; }

        [BsonElement("amount")]
        public decimal Amount { get; set; }

        [BsonElement("currency")]
        public string Currency { get; set; } = "VND";

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public PaymentStatus Status { get; set; }

        [BsonElement("raw")]
        public Dictionary<string, string>? Raw { get; set; }
    }

    public enum PaymentAction
    {
        AUTHORIZE,
        CAPTURE,
        REFUND,
        CANCEL
    }

    public enum PaymentStatus
    {
        PENDING,
        SUCCEEDED,
        FAILED,
        CANCELLED
    }
}

