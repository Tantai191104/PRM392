using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SharedKernel.Entities;

namespace ChatService.Domain.Entities
{
    public class ChatMessage : BaseEntity
    {
        [BsonElement("chat_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string ChatId { get; set; }

        [BsonElement("sender_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string SenderId { get; set; }

        [BsonElement("content")]
        public string? Content { get; set; }

        [BsonElement("attachments")]
        public List<MessageAttachment> Attachments { get; set; } = new();

        [BsonElement("system_flag")]
        public bool SystemFlag { get; set; } = false;

        [BsonElement("read_by")]
        public List<string> ReadBy { get; set; } = new();
    }

    public class MessageAttachment
    {
        [BsonElement("type")]
        public string Type { get; set; } = "image"; // image, file

        [BsonElement("url")]
        public string Url { get; set; } = string.Empty;

        [BsonElement("file_name")]
        public string? FileName { get; set; }

        [BsonElement("file_size")]
        public long? FileSize { get; set; }
    }
}

