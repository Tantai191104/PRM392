namespace OrderService.Application.DTOs
{
    public class OrderDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}