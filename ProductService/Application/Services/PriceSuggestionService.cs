using ProductService.Application.DTOs;

namespace ProductService.Application.Services
{
    public interface IPriceSuggestionService
    {
        Task<PriceSuggestionResponse> GetPriceSuggestionAsync(PriceSuggestionRequest request);
    }

    public class PriceSuggestionService : IPriceSuggestionService
    {
        private readonly ILogger<PriceSuggestionService> _logger;

        // Base prices by brand (mock data)
        private readonly Dictionary<string, decimal> _brandBasePrices = new()
        {
            { "Tesla", 5000000 },
            { "BYD", 4000000 },
            { "LG", 3500000 },
            { "CATL", 3800000 },
            { "Panasonic", 4200000 },
            { "Samsung", 3700000 },
            { "Unknown", 3000000 }
        };

        public PriceSuggestionService(ILogger<PriceSuggestionService> logger)
        {
            _logger = logger;
        }

        public Task<PriceSuggestionResponse> GetPriceSuggestionAsync(PriceSuggestionRequest request)
        {
            var factors = new List<string>();
            
            // Get base price by brand
            var basePrice = _brandBasePrices.GetValueOrDefault(request.Brand ?? "Unknown", 3000000m);
            factors.Add($"Brand: {request.Brand ?? "Unknown"} (Base: {basePrice:N0} VND)");

            // Age factor
            var currentYear = DateTime.UtcNow.Year;
            var age = currentYear - (request.Year ?? currentYear);
            var ageFactor = Math.Max(0.5m, 1m - (age * 0.08m)); // 8% depreciation per year
            basePrice *= ageFactor;
            factors.Add($"Age: {age} years (Factor: {ageFactor:P0})");

            // Cycle count factor
            if (request.CycleCount.HasValue)
            {
                var cycleFactor = request.CycleCount.Value switch
                {
                    < 100 => 1.0m,
                    < 300 => 0.9m,
                    < 500 => 0.8m,
                    < 800 => 0.7m,
                    < 1000 => 0.6m,
                    _ => 0.5m
                };
                basePrice *= cycleFactor;
                factors.Add($"Cycle count: {request.CycleCount} (Factor: {cycleFactor:P0})");
            }

            // SOH (State of Health) factor
            if (request.SOH.HasValue)
            {
                var sohFactor = (decimal)(request.SOH.Value / 100.0);
                basePrice *= sohFactor;
                factors.Add($"SOH: {request.SOH:F1}% (Factor: {sohFactor:P0})");
            }

            // Condition factor
            var conditionFactor = request.Condition?.ToLower() switch
            {
                "new" or "mới" => 1.0m,
                "like new" or "như mới" => 0.95m,
                "good" or "tốt" => 0.85m,
                "fair" or "khá" => 0.75m,
                "poor" or "kém" => 0.6m,
                _ => 0.8m
            };
            basePrice *= conditionFactor;
            factors.Add($"Condition: {request.Condition ?? "Unknown"} (Factor: {conditionFactor:P0})");

            // Calculate price range (±15%)
            var suggestedPrice = Math.Round(basePrice, -4); // Round to nearest 10,000
            var minPrice = Math.Round(suggestedPrice * 0.85m, -4);
            var maxPrice = Math.Round(suggestedPrice * 1.15m, -4);

            var response = new PriceSuggestionResponse
            {
                SuggestedPrice = suggestedPrice,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                PriceRange = $"{minPrice:N0} - {maxPrice:N0} VND",
                Factors = factors,
                Explanation = $"Giá đề xuất dựa trên thương hiệu, năm sản xuất, số chu kỳ sạc, tình trạng pin. " +
                             $"Giá trung bình thị trường cho sản phẩm tương tự là {suggestedPrice:N0} VND. " +
                             $"Bạn có thể điều chỉnh trong khoảng {minPrice:N0} - {maxPrice:N0} VND tùy thuộc vào nhu cầu bán nhanh hay tối đa hóa lợi nhuận."
            };

            _logger.LogInformation("Price suggestion calculated: {Price} VND for {Brand} {Year}", 
                suggestedPrice, request.Brand, request.Year);

            return Task.FromResult(response);
        }
    }
}

