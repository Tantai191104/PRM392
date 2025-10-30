using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SharedKernel.Entities;

namespace ChatService.Domain.Entities
{
    public class Chat : BaseEntity
    {

        [BsonElement("listing_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string ListingId { get; set; }

        [BsonElement("buyer_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string BuyerId { get; set; }

        [BsonElement("seller_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string SellerId { get; set; }

        [BsonElement("order_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? OrderId { get; set; }

        [BsonElement("last_message_at")]
        public DateTime? LastMessageAt { get; set; }

        [BsonElement("buyer_unread_count")]
        public int BuyerUnreadCount { get; set; } = 0;

        [BsonElement("seller_unread_count")]
        public int SellerUnreadCount { get; set; } = 0;
    }
}

