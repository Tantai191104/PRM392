namespace EscrowService.Infrastructure.ExternalServices
{
    public interface IProductServiceClient
    {
        Task<bool> UpdateListingStatusAsync(string listingId, string status);
        Task<ProductDto?> GetProductAsync(string listingId);
    }

    public class ProductDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
    }
}

