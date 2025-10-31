using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace OrderService.Infrastructure.ExternalServices
{
    public class EscrowServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public EscrowServiceClient(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _baseUrl = config["ExternalServices:EscrowService:BaseUrl"] ?? "http://escrowservice:5141";
        }

        public async Task<HttpResponseMessage> CreateEscrowAsync(object escrowDto, string jwtToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/escrows")
            {
                Content = System.Net.Http.Json.JsonContent.Create(escrowDto)
            };
            if (!string.IsNullOrEmpty(jwtToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken.Replace("Bearer ", ""));
            }
            return await _httpClient.SendAsync(request);
        }
    }
}
