using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.ExternalServices
{
    public class WalletServiceClient
    {
        private readonly HttpClient _httpClient;
        public WalletServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<decimal?> GetBalanceAsync(string userId)
        {
            var response = await _httpClient.GetAsync($"/api/wallets/user/{userId}");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("balance", out var balanceProp))
                return balanceProp.GetDecimal();
            return null;
        }
        public async Task<bool> HoldMoneyAsync(string userId, decimal amount)
        {
            var payload = new { UserId = userId, Amount = amount };
            var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/wallets/hold", content);
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> ReleaseMoneyAsync(string userId, decimal amount)
        {
            var payload = new { UserId = userId, Amount = amount };
            var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/wallets/release", content);
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> TransferAsync(string fromUserId, string toUserId, decimal amount)
        {
            var payload = new { FromUserId = fromUserId, ToUserId = toUserId, Amount = amount };
            var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/wallets/transfer", content);
            return response.IsSuccessStatusCode;
        }
    }
}