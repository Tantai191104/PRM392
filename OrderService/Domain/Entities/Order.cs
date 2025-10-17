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

        // Buyer/Seller
        public User Buyer { get; set; } = new User();
        public User Seller { get; set; } = new User();

        // Product
        public Product Product { get; set; } = new Product();
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public void UpdateStatus(OrderStatus newStatus)
        {
            if (newStatus <= Status)
                throw new InvalidOperationException("Order status can only progress forward.");

            Status = newStatus;
        }
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

    public class Product
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Voltage { get; set; } = string.Empty;
        public int CycleCount { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Warranty { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public int Year { get; set; }
        public List<string> Images { get; set; } = new();
        public string Description { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
    }

    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}