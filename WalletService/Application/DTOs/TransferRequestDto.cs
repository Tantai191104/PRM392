namespace WalletService.Application.DTOs
{
    public class TransferRequestDto
    {
        public string? FromUserId { get; set; }
        public string? ToUserId { get; set; }
        public decimal Amount { get; set; }
    }
}
