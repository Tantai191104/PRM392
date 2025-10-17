using OrderService.Application.DTOs;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OrderService.Infrastructure.ExternalServices
{
    public interface IAuthService
    {
        Task<UserDto?> GetUserAsync(string userId);
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public AuthService(HttpClient httpClient, ILogger<AuthService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<UserDto?> GetUserAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Getting user with ID: {UserId}", userId);
                var response = await _httpClient.GetAsync($"/api/users/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<UserDto>(content, _jsonOptions);
                    _logger.LogInformation("Successfully retrieved user: {UserEmail}", user?.Email);
                    return user;
                }
                _logger.LogWarning("User not found. UserId: {UserId}, StatusCode: {StatusCode}", userId, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID: {UserId}", userId);
                throw;
            }
        }
    }
}