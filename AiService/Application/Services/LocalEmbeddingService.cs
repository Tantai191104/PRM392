using System.Text;
using System.Text.Json;

namespace AiService.Application.Services
{
    /// <summary>
    /// Local Embedding Service - Calls Python service running sentence-transformers locally
    /// No external API calls - everything runs on your infrastructure
    /// </summary>
    public class LocalEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LocalEmbeddingService> _logger;
        private readonly string _embeddingServiceUrl;

        public LocalEmbeddingService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<LocalEmbeddingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _embeddingServiceUrl = configuration["LocalEmbedding:ServiceUrl"] 
                ?? "http://localhost:5555";
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                _logger.LogInformation("Generating embedding locally for text: {Text}", 
                    text.Substring(0, Math.Min(50, text.Length)));

                var requestBody = new { text = text };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_embeddingServiceUrl}/embed", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Local embedding service error: {Error}", error);
                    throw new Exception($"Local embedding service error: {error}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<EmbeddingResponse>(responseContent);

                if (result?.Embedding == null)
                {
                    throw new Exception("Invalid response from local embedding service");
                }

                _logger.LogInformation("Successfully generated embedding with {Dimensions} dimensions", 
                    result.Dimensions);

                return result.Embedding;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to connect to local embedding service at {Url}", _embeddingServiceUrl);
                throw new Exception($"Cannot connect to local embedding service at {_embeddingServiceUrl}. Make sure Python service is running.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding locally");
                throw;
            }
        }

        private class EmbeddingResponse
        {
            public float[] Embedding { get; set; } = Array.Empty<float>();
            public int Dimensions { get; set; }
        }
    }
}
