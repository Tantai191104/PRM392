using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace ProductService.Application.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(HttpClient httpClient, string endpoint, string apiKey, ILogger<GeminiService> logger)
        {
            // Đặt timeout nếu chưa cấu hình
            if (httpClient.Timeout.TotalSeconds > 30 || httpClient.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
                httpClient.Timeout = TimeSpan.FromSeconds(15);

            _httpClient = httpClient;
            _endpoint = endpoint;
            _apiKey = apiKey;
            _logger = logger;
        }

        /// <summary>
        /// Trả về endpoint kèm API key
        /// </summary>
        public string GetEndpointWithKey()
        {
            return _endpoint.Contains("?")
                ? _endpoint + "&key=" + _apiKey
                : _endpoint + "?key=" + _apiKey;
        }

        /// <summary>
        /// Gửi POST request tới Gemini API với body đã chuẩn hóa
        /// </summary>
        public async Task<HttpResponseMessage> PostToGeminiAsync(string endpointWithKey, HttpContent content)
        {
            try
            {
                _logger.LogInformation("[GeminiService] Sending POST request to {Endpoint}", endpointWithKey);
                var response = await _httpClient.PostAsync(endpointWithKey, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("[GeminiService] Response status: {Status}", response.StatusCode);
                if (!response.IsSuccessStatusCode)
                {
                    var safeKey = _apiKey.Length > 6 ? _apiKey.Substring(0, 6) + "..." : _apiKey;
                    _logger.LogWarning("[GeminiService] Request failed: {Status} - {Body} - APIKey: {SafeKey}", response.StatusCode, responseBody, safeKey);
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GeminiService] Exception during PostToGeminiAsync");
                throw;
            }
        }

        /// <summary>
        /// Gọi Gemini API để đánh giá SOH của pin (0-100%)
        /// </summary>
        public async Task<double> EvaluateSOHAsync(object batteryData)
        {
            try
            {
                // Prompt rõ ràng yêu cầu AI trả duy nhất một số 0-100
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = $"Đánh giá SOH của pin với dữ liệu: {JsonSerializer.Serialize(batteryData)}. " +
                                           "Chỉ trả về duy nhất một số từ 0 đến 100, không thêm chữ hay ký tự %, không thêm text nào khác."
                                }
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var endpointWithKey = GetEndpointWithKey();

                _logger.LogInformation("[GeminiService] Sending request to Gemini API for SOH evaluation...");

                var response = await _httpClient.PostAsync(endpointWithKey, content);
                var responseString = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("[GeminiService] Response status: {Status}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var safeKey = _apiKey.Length > 6 ? _apiKey.Substring(0, 6) + "..." : _apiKey;
                    _logger.LogWarning("Gemini API call failed: {Status} - {Body} - APIKey: {SafeKey}", response.StatusCode, responseString, safeKey);
                    return 100; // fallback nếu lỗi
                }

                var json = JsonDocument.Parse(responseString);
                var candidates = json.RootElement.GetProperty("candidates");

                if (candidates.GetArrayLength() == 0)
                    return 100;

                var parts = candidates[0].GetProperty("content").GetProperty("parts");
                var text = parts[0].GetProperty("text").GetString() ?? "100";

                // Lấy số cuối cùng trong text, đảm bảo đúng 0-100
                double sohValue = 100;
                var matches = Regex.Matches(text, @"\d+(\.\d+)?");
                if (matches.Count > 0)
                {
                    sohValue = double.Parse(matches[matches.Count - 1].Value);
                    if (sohValue < 0) sohValue = 0;
                    if (sohValue > 100) sohValue = 100;
                }

                return sohValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return 100; // fallback nếu exception
            }
        }
    }
}
