using System.Text;
using System.Text.Json;

namespace EscrowService.Infrastructure.ExternalServices
{
    public class ProductServiceClient : IProductServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductServiceClient> _logger;

        public ProductServiceClient(HttpClient httpClient, ILogger<ProductServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> UpdateListingStatusAsync(string listingId, string status)
        {
            try
            {
                var payload = new { status };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"/api/products/{listingId}/status", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Updated listing {ListingId} status to {Status}", listingId, status);
                    return true;
                }
                
                _logger.LogWarning("Failed to update listing {ListingId} status: {StatusCode}", 
                    listingId, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating listing {ListingId} status", listingId);
                return false;
            }
        }

        public async Task<ProductDto?> GetProductAsync(string listingId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/products/{listingId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var product = JsonSerializer.Deserialize<ProductDto>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return product;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {ListingId}", listingId);
                return null;
            }
        }
    }
}

