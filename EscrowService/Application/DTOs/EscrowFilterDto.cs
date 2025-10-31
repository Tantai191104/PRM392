namespace EscrowService.Application.DTOs
{
    public class EscrowFilterDto
    {
        public string? Status { get; set; }
        public string? BuyerId { get; set; }
        public string? SellerId { get; set; }
        public string? OrderId { get; set; }
    }
}
