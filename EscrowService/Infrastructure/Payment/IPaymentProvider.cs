namespace EscrowService.Infrastructure.Providers
{
    public interface IPaymentProvider
    {
        Task<string> AuthorizeAsync(string customerId, decimal amount);
        Task CaptureAsync(string paymentIntentId);
        Task RefundAsync(string paymentIntentId, decimal amount);
        Task CancelAsync(string paymentIntentId);
    }
}

