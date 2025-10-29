using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProductService.Domain.Entities
{
    public class Product
    {
        public string Brand { get; set; } = string.Empty;
        public string Voltage { get; set; } = string.Empty;
        public int CycleCount { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Warranty { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft"; // Draft, Pending, Published, InTransaction, Sold, Expired, Rejected
        public double? SOH { get; set; } // State of Health (%)
        public string ListingType { get; set; } = "FixedPrice"; // FixedPrice, Auction
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string Name { get; set; } = null!;     // Tên sản phẩm
        public string Type { get; set; } = string.Empty; // Loại pin
        public string Capacity { get; set; } = string.Empty; // Dung lượng (Ah, Wh)
        public string Condition { get; set; } = string.Empty; // Tình trạng (Mới, Đã sử dụng, ...)
        public int Year { get; set; } // Năm sản xuất
        public decimal Price { get; set; } // Giá bán
        public List<string> Images { get; set; } = new(); // Danh sách URL hình ảnh
        public string Description { get; set; } = string.Empty; // Mô tả chi tiết
        public string OwnerId { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? RejectionReason { get; set; } // Lý do từ chối
    }
}
