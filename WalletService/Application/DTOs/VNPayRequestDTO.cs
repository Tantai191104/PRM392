namespace WalletService.Application.DTOs
{
    public class VNPayRequestDTO
    {
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? OrderInfo { get; set; }
        public string? ReturnUrl { get; set; }
    }
}