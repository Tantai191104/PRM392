using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace EscrowService.Infrastructure.ExternalServices
{
    public class WalletServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WalletServiceClient> _logger;

        public WalletServiceClient(HttpClient httpClient, ILogger<WalletServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
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