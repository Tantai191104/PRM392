using OrderService.Application.DTOs;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OrderService.Infrastructure.ExternalServices
{
    public interface IProductService
    {
        Task<ProductDto?> GetProductAsync(string productId);
    }

    public class ProductService : IProductService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ProductService(HttpClient httpClient, ILogger<ProductService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<ProductDto?> GetProductAsync(string productId)
        {
            try
            {
                _logger.LogInformation("Getting product with ID: {ProductId}", productId);
                var response = await _httpClient.GetAsync($"/api/products/{productId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var product = JsonSerializer.Deserialize<ProductDto>(content, _jsonOptions);
                    _logger.LogInformation("Successfully retrieved product: {ProductName}", product?.Name);
                    return product;
                }
                _logger.LogWarning("Product not found. ProductId: {ProductId}, StatusCode: {StatusCode}", productId, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product with ID: {ProductId}", productId);
                throw;
            }
        }
    }
}