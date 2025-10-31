using EscrowService.Domain.Entities;

namespace EscrowService.Application.DTOs
{
    public class CreateEscrowDto
    {
    public string? OrderId { get; set; }
    public object? Order { get; set; }
        public required string ProductId { get; set; }
        public required string SellerId { get; set; }
        public decimal Amount { get; set; }
    }

    public class EscrowResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string? OrderId { get; set; }
        public object? Order { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string BuyerId { get; set; } = string.Empty;
        public string SellerId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal AmountTotal { get; set; }
        public decimal AmountHold { get; set; }
        public string Currency { get; set; } = string.Empty;
        public PaymentInfo? Payment { get; set; }
        public List<EscrowEvent> Events { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ReleaseEscrowDto
    {
        public string? Reason { get; set; }
    }

    public class RefundEscrowDto
    {
        public required string Reason { get; set; }
    }
}

