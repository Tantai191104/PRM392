using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AiService.Application.Services
{
    /// <summary>
    /// Free embedding service using Hugging Face Inference API
    /// Model: sentence-transformers/all-MiniLM-L6-v2 (384 dimensions)
    /// </summary>
    public class HuggingFaceEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private readonly ILogger<HuggingFaceEmbeddingService> _logger;

        public HuggingFaceEmbeddingService(
            IConfiguration configuration,
            ILogger<HuggingFaceEmbeddingService> logger)
        {
            _logger = logger;
            
            var apiKey = configuration["HuggingFace:ApiKey"];
            _model = configuration["HuggingFace:EmbeddingModel"] 
                ?? "sentence-transformers/all-MiniLM-L6-v2";
            
            _httpClient = new HttpClient();
            
            // API key is optional for public models but recommended to avoid rate limits
            if (!string.IsNullOrEmpty(apiKey) && apiKey != "your-huggingface-api-key-here")
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", apiKey);
            }
            
            _logger.LogInformation("HuggingFace Embedding Service initialized with model: {Model}", _model);
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                _logger.LogInformation("Generating embedding for text: {Text}", 
                    text.Length > 100 ? text.Substring(0, 100) + "..." : text);

                var url = $"https://api-inference.huggingface.co/pipeline/feature-extraction/{_model}";
                
                var requestBody = new
                {
                    inputs = text,
                    options = new { wait_for_model = true }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("HuggingFace API error: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    throw new Exception($"HuggingFace API error: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Parse response - HuggingFace returns nested array for sentence-transformers
                var embedding = JsonSerializer.Deserialize<float[][]>(responseContent);
                
                if (embedding == null || embedding.Length == 0)
                {
                    throw new Exception("Failed to parse embedding from response");
                }

                // Take first embedding (for single sentence)
                var result = embedding[0];
                
                _logger.LogInformation("Embedding generated successfully with {Dimensions} dimensions", 
                    result.Length);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding with HuggingFace");
                throw;
            }
        }
    }
}
