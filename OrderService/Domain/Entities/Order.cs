using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SharedKernel.Entities;

namespace OrderService.Domain.Entities
{
    public class Order : BaseEntity
    {
        [BsonElement("buyer")]
        public User Buyer { get; set; } = new();

        [BsonElement("seller")]
        public User Seller { get; set; } = new();

        [BsonElement("product")]
        public Product Product { get; set; } = new();

        [BsonElement("totalAmount")]
        public decimal TotalAmount { get; set; }

        [BsonElement("shippingFee")]
        public decimal ShippingFee { get; set; } = 30000;

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [BsonElement("paymentMethod")]
        public string PaymentMethod { get; set; } = "Cash"; // hoặc "BankTransfer", "COD"

        [BsonElement("shippingAddress")]
        public string ShippingAddress { get; set; } = string.Empty;

        [BsonElement("notes")]
        public string Notes { get; set; } = string.Empty;

        [BsonElement("timeline")]
        public List<OrderTimelineEntry> Timeline { get; set; } = new();

        public void UpdateStatus(OrderStatus newStatus, string updatedById, string updatedBy)
        {
            if (string.IsNullOrWhiteSpace(updatedById))
                throw new ArgumentNullException(nameof(updatedById));

            if (string.IsNullOrWhiteSpace(updatedBy))
                throw new ArgumentNullException(nameof(updatedBy));

            if (!IsValidTransition(Status, newStatus))
                throw new InvalidOperationException($"Invalid status change from {Status} to {newStatus}");

            Timeline.Add(new OrderTimelineEntry
            {
                FromStatus = Status.ToString(),
                ToStatus = newStatus.ToString(),
                UpdatedById = updatedById,
                UpdatedBy = updatedBy,
                UpdatedAt = DateTime.UtcNow
            });

            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }

        private static bool IsValidTransition(OrderStatus from, OrderStatus to)
        {
            return from switch
            {
                OrderStatus.Pending => to is OrderStatus.Confirmed or OrderStatus.Cancelled,
                OrderStatus.Confirmed => to is OrderStatus.Processing or OrderStatus.Cancelled,
                OrderStatus.Processing => to is OrderStatus.Shipped or OrderStatus.Cancelled,
                OrderStatus.Shipped => to is OrderStatus.Delivered or OrderStatus.Cancelled,
                _ => false
            };
        }

        public void Cancel(string cancelledById, string cancelledBy)
        {
            if (string.IsNullOrWhiteSpace(cancelledById))
                throw new ArgumentNullException(nameof(cancelledById));

            if (string.IsNullOrWhiteSpace(cancelledBy))
                throw new ArgumentNullException(nameof(cancelledBy));

            if (Status != OrderStatus.Pending)
                throw new InvalidOperationException("Only pending orders can be cancelled.");

            Timeline.Add(new OrderTimelineEntry
            {
                FromStatus = Status.ToString(),
                ToStatus = OrderStatus.Cancelled.ToString(),
                UpdatedById = cancelledById,
                UpdatedBy = cancelledBy,
                UpdatedAt = DateTime.UtcNow
            });

            Status = OrderStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public class OrderTimelineEntry
    {
        [BsonElement("fromStatus")]
        public string FromStatus { get; set; } = string.Empty;

        [BsonElement("toStatus")]
        public string ToStatus { get; set; } = string.Empty;

        [BsonElement("updatedById")]
        public string UpdatedById { get; set; } = string.Empty;

        [BsonElement("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
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
        [BsonElement("id")]
        public string Id { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("brand")]
        public string Brand { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        [BsonElement("capacity")]
        public string Capacity { get; set; } = string.Empty;

        [BsonElement("condition")]
        public string Condition { get; set; } = string.Empty;

        [BsonElement("year")]
        public int Year { get; set; }

        [BsonElement("price")]
        public decimal Price { get; set; }

        [BsonElement("voltage")]
        public string Voltage { get; set; } = string.Empty;

        [BsonElement("cycleCount")]
        public int CycleCount { get; set; }

        [BsonElement("location")]
        public string Location { get; set; } = string.Empty;

        [BsonElement("warranty")]
        public string Warranty { get; set; } = string.Empty;

        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;

        [BsonElement("images")]
        public List<string> Images { get; set; } = new();

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("ownerId")]
        public string OwnerId { get; set; } = string.Empty;
    }

    public class User
    {
        [BsonElement("id")]
        public string Id { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("phone")]
        public string Phone { get; set; } = string.Empty;
    }
}
