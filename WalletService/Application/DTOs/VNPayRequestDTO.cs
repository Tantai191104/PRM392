namespace WalletService.Application.DTOs
{
    public class VNPayRequestDTO
    {
        public string UserId { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string OrderType { get; set; } = "140000";
        public string OrderDescription { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
    }
}