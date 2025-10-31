using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SharedKernel.Entities;

namespace EscrowService.Domain.Entities
{
    public class Escrow : BaseEntity
    {
        [BsonElement("order_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? OrderId { get; set; }

        [BsonElement("product_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string ProductId { get; set; }

        [BsonElement("buyer_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string BuyerId { get; set; }

        [BsonElement("seller_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string SellerId { get; set; }

        [BsonElement("terms")]
        public EscrowTerms Terms { get; set; } = new();

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public EscrowStatus Status { get; set; } = EscrowStatus.CREATED;

        [BsonElement("currency")]
        public string Currency { get; set; } = "VND";

        [BsonElement("amount_total")]
        public decimal AmountTotal { get; set; }

        [BsonElement("amount_hold")]
        public decimal AmountHold { get; set; }

        [BsonElement("payment")]
        public PaymentInfo? Payment { get; set; }

        [BsonElement("payout")]
        public PayoutInfo? Payout { get; set; }

        [BsonElement("events")]
        public List<EscrowEvent> Events { get; set; } = new();

        public void AddEvent(EscrowEventType eventType, string description, string? byUserId = null)
        {
            Events.Add(new EscrowEvent
            {
                Type = eventType,
                At = DateTime.UtcNow,
                By = byUserId,
                Description = description
            });
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class EscrowTerms
    {
        [BsonElement("auto_release_at")]
        public DateTime? AutoReleaseAt { get; set; }

        [BsonElement("release_conditions")]
        public List<string> ReleaseConditions { get; set; } = new();
    }

    public class PaymentInfo
    {
        [BsonElement("provider")]
        public string Provider { get; set; } = "Mock";

        [BsonElement("payment_intent_id")]
        public string? PaymentIntentId { get; set; }

        [BsonElement("authorized_at")]
        public DateTime? AuthorizedAt { get; set; }

        [BsonElement("captured_at")]
        public DateTime? CapturedAt { get; set; }

        [BsonElement("refunded_at")]
        public DateTime? RefundedAt { get; set; }
    }

    public class PayoutInfo
    {
        [BsonElement("seller_account_id")]
        public string? SellerAccountId { get; set; }

        [BsonElement("payout_status")]
        public string? PayoutStatus { get; set; }

        [BsonElement("payout_at")]
        public DateTime? PayoutAt { get; set; }
    }

    public class EscrowEvent
    {
        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public EscrowEventType Type { get; set; }

        [BsonElement("at")]
        public DateTime At { get; set; }

        [BsonElement("by")]
        public string? By { get; set; }

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("meta")]
        public Dictionary<string, string>? Meta { get; set; }
    }

    public enum EscrowStatus
    {
        CREATED,
        AUTHORIZED,
        CAPTURED,
        HOLDING,
        RELEASED,
        REFUNDED,
        DISPUTED,
        RESOLVED,
        FAILED
    }

    public enum EscrowEventType
    {
        CREATED,
        AUTHORIZED,
        CAPTURED,
        BUYER_CONFIRMED,
        SELLER_CONFIRMED,
        RELEASED,
        REFUNDED,
        DISPUTED,
        RESOLVED,
        FAILED
    }
}

