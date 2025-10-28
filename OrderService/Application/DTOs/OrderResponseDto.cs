namespace OrderService.Application.DTOs
{
    public class OrderResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public ProductDto Product { get; set; } = new();
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public UserBriefDto Buyer { get; set; } = new();
        public UserBriefDto Seller { get; set; } = new();
        public List<OrderTimelineDto> Timeline { get; set; } = new();
    }

    public class OrderTimelineDto
    {
        public string FromStatus { get; set; } = string.Empty;
        public string ToStatus { get; set; } = string.Empty;
        public string UpdatedById { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

}
