using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AuthService.Application.Services
{
    public class WalletClientService
    {
        private readonly HttpClient _httpClient;
        public WalletClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task CreateWalletAsync(string userId)
        {
            var payload = new { UserId = userId, Balance = 0M };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("/api/wallets", content);
        }
    }
}