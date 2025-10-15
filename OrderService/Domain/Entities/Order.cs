using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SharedKernel.Entities;

namespace OrderService.Domain.Entities
{
    public class Order : BaseEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public new string Id { get; set; } = string.Empty;
        
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        // User and Product info cached from other services
        public string UserEmail { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
    }

    public enum OrderStatus
    {
        Pending = 0,
        Confirmed = 1,
        Processing = 2,
        Shipped = 3,
        Delivered = 4,
        Cancelled = 5
    }
}