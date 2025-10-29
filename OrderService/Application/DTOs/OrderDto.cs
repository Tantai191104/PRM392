namespace OrderService.Application.DTOs
{
    public class OrderDto
    {
        // ID sản phẩm được mua
        public string ProductId { get; set; } = string.Empty;

        // Phương thức thanh toán: "Cash", "BankTransfer", "COD"
        public string PaymentMethod { get; set; } = "Cash";

        // Địa chỉ giao hàng của người mua
        public string ShippingAddress { get; set; } = string.Empty;

        // Ghi chú của khách hàng (ví dụ: "Giao buổi sáng", "Không gọi khi giao")
        public string Notes { get; set; } = string.Empty;

        // Phí ship (mặc định 30000 VNĐ)
        public decimal ShippingFee { get; set; } = 30000;
    }
}
