namespace EscrowService.Infrastructure.ExternalServices
{
    public interface IOrderServiceClient
    {
    Task<string?> CreateOrderAsync(string buyerId, string listingId, string escrowId);
    Task<bool> CancelOrderAsync(string orderId);
    Task<string?> GetOrderStatusAsync(string orderId);
    }
}

