using ChatService.Domain.Entities;

namespace ChatService.Application.DTOs
{
    public class CreateChatDto
    {
        public required string ListingId { get; set; }
        public required string SellerId { get; set; }
    }

    public class ChatResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string ListingId { get; set; } = string.Empty;
        public string BuyerId { get; set; } = string.Empty;
        public string SellerId { get; set; } = string.Empty;
        public string? OrderId { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int BuyerUnreadCount { get; set; }
        public int SellerUnreadCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public ChatMessageDto? LastMessage { get; set; }
    }

    public class SendMessageDto
    {
        public string? Content { get; set; }
        public List<MessageAttachment>? Attachments { get; set; }
    }

    public class ChatMessageDto
    {
        public string Id { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string? Content { get; set; }
        public List<MessageAttachment> Attachments { get; set; } = new();
        public bool SystemFlag { get; set; }
        public List<string> ReadBy { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class MarkReadDto
    {
        public required string ChatId { get; set; }
    }
}

