using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProductService.Domain.Entities
{
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string Name { get; set; } = null!;     // tên pin

        public decimal Price { get; set; }            // giá bán

    public Dictionary<string, object?> Attributes { get; set; } = new();
        // lưu các thuộc tính động: dung lượng, tình trạng, năm sản xuất,…
        public string OwnerId { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
