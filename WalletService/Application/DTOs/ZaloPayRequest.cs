namespace WalletService.Application.DTOs
{
    public class ZaloPayRequest
    {
        public string AppId { get; set; } = string.Empty;
        public string AppUser { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string EmbedData { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
    }
}