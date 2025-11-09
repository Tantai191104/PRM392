using APIGateway.DTOs;
using System.Text.Json;

namespace APIGateway.Services
{
    public class DashboardService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DashboardService> _logger;
        private readonly CacheService _cacheService;

        public DashboardService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<DashboardService> logger,
            CacheService cacheService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _cacheService = cacheService;
        }

        // Helper method to build query string
        private string BuildQueryString(DateTime? startDate, DateTime? endDate)
        {
            var queryParams = new List<string>();
            
            if (startDate.HasValue)
                queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
            if (endDate.HasValue)
                queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
            
            return queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        }

        // ============ MAIN OVERVIEW ============
        public async Task<DashboardOverviewDto> GetOverviewAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var overview = new DashboardOverviewDto();

            try
            {
                // Parallel requests to all services
                var userStatsTask = GetUserStatsAsync(startDate, endDate);
                var productStatsTask = GetProductStatsAsync(startDate, endDate);
                var orderStatsTask = GetOrderStatsAsync(startDate, endDate);
                var walletStatsTask = GetWalletStatsAsync(startDate, endDate);
                var revenueStatsTask = GetRevenueStatsAsync(startDate, endDate);

                await Task.WhenAll(userStatsTask, productStatsTask, orderStatsTask, walletStatsTask, revenueStatsTask);
                
                overview.Users = await userStatsTask;
                overview.Products = await productStatsTask;
                overview.Orders = await orderStatsTask;
                overview.Wallets = await walletStatsTask;
                overview.Revenue = await revenueStatsTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard overview");
            }

            return overview;
        }

        // ============ USER STATS ============
        public async Task<UserStats> GetUserStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var authServiceUrl = _configuration["Services:AuthService"] ?? "http://authservice:8080";
                var queryString = BuildQueryString(startDate, endDate);
                var response = await _httpClient.GetAsync($"{authServiceUrl}/api/dashboard/users{queryString}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserStats>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                           ?? new UserStats();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats");
            }

            return new UserStats();
        }

        // ============ PRODUCT STATS ============
        public async Task<ProductStats> GetProductStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var productServiceUrl = _configuration["Services:ProductService"] ?? "http://productservice:8080";
                var queryString = BuildQueryString(startDate, endDate);
                var response = await _httpClient.GetAsync($"{productServiceUrl}/api/dashboard/products{queryString}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ProductStats>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                           ?? new ProductStats();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product stats");
            }

            return new ProductStats();
        }

        // ============ ORDER STATS ============
        public async Task<OrderStats> GetOrderStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://orderservice:8080";
                var queryString = BuildQueryString(startDate, endDate);
                var response = await _httpClient.GetAsync($"{orderServiceUrl}/api/dashboard/orders{queryString}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<OrderStats>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                           ?? new OrderStats();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order stats");
            }

            return new OrderStats();
        }

        // ============ WALLET STATS ============
        public async Task<WalletStats> GetWalletStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var walletServiceUrl = _configuration["Services:WalletService"] ?? "http://walletservice:8080";
                var queryString = BuildQueryString(startDate, endDate);
                var response = await _httpClient.GetAsync($"{walletServiceUrl}/api/dashboard/wallets{queryString}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<WalletStats>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                           ?? new WalletStats();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallet stats");
            }

            return new WalletStats();
        }

        // ============ REVENUE STATS ============
        public async Task<RevenueStats> GetRevenueStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Revenue can come from multiple sources
                var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://orderservice:8080";
                var queryString = BuildQueryString(startDate, endDate);
                var response = await _httpClient.GetAsync($"{orderServiceUrl}/api/dashboard/revenue{queryString}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<RevenueStats>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                           ?? new RevenueStats();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue stats");
            }

            return new RevenueStats();
        }

        // ============ TOP STATS ============
        public async Task<TopStatsDto> GetTopStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var topStats = new TopStatsDto();

            try
            {
                var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://orderservice:8080";
                var queryString = BuildQueryString(startDate, endDate);
                var response = await _httpClient.GetAsync($"{orderServiceUrl}/api/dashboard/top-stats{queryString}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<TopStatsDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                           ?? new TopStatsDto();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top stats");
            }

            return topStats;
        }

        // ============ RECENT ACTIVITIES ============
        public async Task<RecentActivitiesDto> GetRecentActivitiesAsync(int limit = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            var activities = new RecentActivitiesDto();

            try
            {
                // Get recent data from multiple services in parallel
                var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://orderservice:8080";
                var productServiceUrl = _configuration["Services:ProductService"] ?? "http://productservice:8080";
                var authServiceUrl = _configuration["Services:AuthService"] ?? "http://authservice:8080";

                var queryString = BuildQueryString(startDate, endDate);
                var limitParam = string.IsNullOrEmpty(queryString) ? $"?limit={limit}" : $"{queryString}&limit={limit}";

                var tasks = new[]
                {
                    _httpClient.GetAsync($"{orderServiceUrl}/api/dashboard/recent-orders{limitParam}"),
                    _httpClient.GetAsync($"{productServiceUrl}/api/dashboard/recent-products{limitParam}"),
                    _httpClient.GetAsync($"{authServiceUrl}/api/dashboard/recent-users{limitParam}")
                };

                var responses = await Task.WhenAll(tasks);

                if (responses[0].IsSuccessStatusCode)
                {
                    var content = await responses[0].Content.ReadAsStringAsync();
                    activities.RecentOrders = JsonSerializer.Deserialize<List<RecentOrderDto>>(content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<RecentOrderDto>();
                }

                if (responses[1].IsSuccessStatusCode)
                {
                    var content = await responses[1].Content.ReadAsStringAsync();
                    activities.RecentProducts = JsonSerializer.Deserialize<List<RecentProductDto>>(content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<RecentProductDto>();
                }

                if (responses[2].IsSuccessStatusCode)
                {
                    var content = await responses[2].Content.ReadAsStringAsync();
                    activities.RecentUsers = JsonSerializer.Deserialize<List<RecentUserDto>>(content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<RecentUserDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
            }

            return activities;
        }

        // ============ CHART DATA ============
        public async Task<object> GetOrdersChartDataAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://orderservice:8080";
                var queryString = BuildQueryString(startDate, endDate);
                var response = await _httpClient.GetAsync($"{orderServiceUrl}/api/dashboard/orders-chart{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<object>(content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new { };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders chart data");
            }

            return new { };
        }

        public async Task<object> GetRevenueChartDataAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://orderservice:8080";
                var queryString = BuildQueryString(startDate, endDate);
                var response = await _httpClient.GetAsync($"{orderServiceUrl}/api/dashboard/revenue-chart{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<object>(content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new { };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue chart data");
            }

            return new { };
        }

        public async Task<object> GetOrdersByDateRangeAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var orderServiceUrl = _configuration["Services:OrderService"] ?? "http://orderservice:8080";
                var queryString = BuildQueryString(startDate, endDate);
                var response = await _httpClient.GetAsync($"{orderServiceUrl}/api/dashboard/orders-by-date{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<object>(content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new { };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders by date range");
            }

            return new { };
        }
    }
}
