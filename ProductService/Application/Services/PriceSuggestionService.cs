using ProductService.Application.DTOs;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace ProductService.Application.Services
{
    public interface IPriceSuggestionService
    {
        Task<PriceSuggestionResponse> GetPriceSuggestionAsync(PriceSuggestionRequest request);
    }

    public class PriceSuggestionService : IPriceSuggestionService
    {
        private readonly ILogger<PriceSuggestionService> _logger;
        private readonly GeminiService _geminiService;

        // Giá cơ bản theo thương hiệu (VND)
        private readonly Dictionary<string, decimal> _brandBasePrices = new()
        {
            { "Tesla", 5_000_000 },
            { "BYD", 4_000_000 },
            { "LG", 3_500_000 },
            { "CATL", 3_800_000 },
            { "Panasonic", 4_200_000 },
            { "Samsung", 3_700_000 },
            { "Unknown", 3_000_000 }
        };

        public PriceSuggestionService(ILogger<PriceSuggestionService> logger, GeminiService geminiService)
        {
            _logger = logger;
            _geminiService = geminiService;
        }

        // Private method: gọi Gemini API để lấy giá gợi ý
        private async Task<decimal> GetAiSuggestedPriceAsync(PriceSuggestionRequest request)
        {
            try
            {
                // Chỉ truyền thông tin product, không truyền SOH
                // Chỉ truyền các trường quan trọng để AI đánh giá chính xác
                // Chỉ lấy các trường có trong ProductDto
                var productInfo = new
                {
                    Brand = request.Brand,
                    Year = request.Year,
                    CycleCount = request.CycleCount,
                    Capacity = request.Capacity,
                    Condition = request.Condition,
                    Voltage = request.Voltage
                };

                var pricePrompt = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = $"Dựa trên thông tin sản phẩm sau hãy đánh giá SOH của pin và đề xuất giá bán hợp lý cho pin: {JsonSerializer.Serialize(productInfo)}. " +
                                           "Chỉ trả về duy nhất một số nguyên là giá bán đề xuất (VND), không thêm text nào khác."
                                }
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(pricePrompt), Encoding.UTF8, "application/json");
                var endpointWithKey = _geminiService.GetEndpointWithKey();
                var response = await _geminiService.PostToGeminiAsync(endpointWithKey, content);

                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(responseString);

                var candidates = json.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() > 0)
                {
                    var parts = candidates[0].GetProperty("content").GetProperty("parts");
                    var aiText = parts[0].GetProperty("text").GetString() ?? "";
                    var match = Regex.Match(aiText, @"\d+");
                    if (match.Success)
                        return decimal.Parse(match.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get AI suggested price from Gemini.");
            }

            return 0;
        }

        // Public method: tính giá gợi ý
        public async Task<PriceSuggestionResponse> GetPriceSuggestionAsync(PriceSuggestionRequest request)
        {
            var factors = new List<string>();

            decimal aiPrice = 0;
            double soh = 100;
            try
            {
                // Gọi song song Gemini cho SOH và giá gợi ý
                var sohTask = _geminiService.EvaluateSOHAsync(request);
                var priceTask = GetAiSuggestedPriceAsync(request);
                await Task.WhenAll(sohTask, priceTask);
                soh = sohTask.Result;
                aiPrice = priceTask.Result;
                factors.Add($"SOH từ Gemini: {soh:F1}%");
                if (aiPrice > 0)
                    factors.Add($"Giá gợi ý từ Gemini: {aiPrice:N0} VND");
                else
                    factors.Add($"Không lấy được giá từ Gemini, dùng mặc định.");
            }
            catch (Exception ex)
            {
                factors.Add($"Không lấy được SOH hoặc giá từ Gemini, dùng mặc định. Lỗi: {ex.Message}");
            }

            // Nếu AI trả về giá hợp lệ thì dùng, nếu không thì fallback về logic cũ
            decimal suggestedPrice = aiPrice > 0 ? aiPrice : 3_000_000;
            var minPrice = decimal.Round(suggestedPrice * 0.85m, 0, MidpointRounding.AwayFromZero);
            var maxPrice = decimal.Round(suggestedPrice * 1.15m, 0, MidpointRounding.AwayFromZero);

            var response = new PriceSuggestionResponse
            {
                SuggestedPrice = suggestedPrice,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                PriceRange = $"{minPrice:N0} - {maxPrice:N0} VND",
                Factors = factors,
                Explanation = $"Giá đề xuất dựa trên AI Gemini. " +
                              $"Giá trung bình thị trường cho sản phẩm tương tự là {suggestedPrice:N0} VND. " +
                              $"Bạn có thể điều chỉnh trong khoảng {minPrice:N0} - {maxPrice:N0} VND tùy thuộc vào nhu cầu bán nhanh hay tối đa hóa lợi nhuận.",
                SOH = soh
            };

            _logger.LogInformation("Price suggestion calculated: {Price} VND for {Brand} {Year} SOH: {SOH}",
                suggestedPrice, request.Brand, request.Year, soh);

            return response;
        }
    }
}
