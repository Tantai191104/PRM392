namespace WalletService.Application.DTOs
{
    public class HoldRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
