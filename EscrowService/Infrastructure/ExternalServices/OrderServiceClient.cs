using System.Text;
using System.Text.Json;

namespace EscrowService.Infrastructure.ExternalServices
{
    public class OrderServiceClient : IOrderServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OrderServiceClient> _logger;

        public OrderServiceClient(HttpClient httpClient, ILogger<OrderServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string?> CreateOrderAsync(string buyerId, string listingId, string escrowId)
        {
            try
            {
                var payload = new { productId = listingId, escrowId };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                // Add buyer ID header
                _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
                _httpClient.DefaultRequestHeaders.Add("X-User-Id", buyerId);

                var response = await _httpClient.PostAsync("/api/orders", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonDocument>(json);
                    var orderId = result?.RootElement.GetProperty("id").GetString();

                    _logger.LogInformation("Created order {OrderId} for listing {ListingId}", orderId, listingId);
                    return orderId;
                }

                _logger.LogWarning("Failed to create order: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for listing {ListingId}", listingId);
                return null;
            }
        }

        public async Task<bool> CancelOrderAsync(string orderId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/api/orders/{orderId}/cancel", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
                return false;
            }
        }
        public async Task<string?> GetOrderStatusAsync(string orderId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/orders/status/{orderId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonDocument>(json);
                    var status = result?.RootElement.GetProperty("status").GetString();

                    _logger.LogInformation("Fetched status '{Status}' for order {OrderId}", status, orderId);
                    return status;
                }

                _logger.LogWarning("Failed to get status for order {OrderId}: {StatusCode}", orderId, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status for order {OrderId}", orderId);
                return null;
            }
        }
    }
}

