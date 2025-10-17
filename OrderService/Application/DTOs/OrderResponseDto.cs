namespace OrderService.Application.DTOs
{
    public class OrderResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public OrderService.Domain.Entities.OrderStatus Status { get; set; }
        public UserDto Buyer { get; set; } = new UserDto();
        public UserDto Seller { get; set; } = new UserDto();
        public string ProductName { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
    }

    // Removed duplicate OrderStatus enum
}
