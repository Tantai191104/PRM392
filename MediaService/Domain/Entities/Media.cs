using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SharedKernel.Entities;

namespace MediaService.Domain.Entities
{
    public class Media : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public override string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("listing_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ListingId { get; set; } = string.Empty;

        [BsonElement("url")]
        public string Url { get; set; } = string.Empty;

        [BsonElement("type")]
        public MediaType Type { get; set; }

        [BsonElement("order")]
        public int Order { get; set; }

        [BsonElement("file_name")]
        public string FileName { get; set; } = string.Empty;

        [BsonElement("file_size")]
        public long FileSize { get; set; }

        [BsonElement("content_type")]
        public string ContentType { get; set; } = string.Empty;

        [BsonElement("storage_path")]
        public string StoragePath { get; set; } = string.Empty;
    }

    public enum MediaType
    {
        IMAGE,
        VIDEO
    }
}

