using APIGateway.DTOs;
using APIGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace APIGateway.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(DashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// 📊 Get complete dashboard overview with all statistics
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        /// <returns>Dashboard overview including users, products, orders, wallets, revenue</returns>
        [HttpGet("overview")]
        [ProducesResponseType(typeof(DashboardOverviewDto), 200)]
        public async Task<IActionResult> GetOverview(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var overview = await _dashboardService.GetOverviewAsync(startDate, endDate);
                return Ok(new
                {
                    success = true,
                    data = overview,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOverview");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// 👥 Get user statistics
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet("users")]
        [ProducesResponseType(typeof(UserStats), 200)]
        public async Task<IActionResult> GetUserStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var stats = await _dashboardService.GetUserStatsAsync(startDate, endDate);
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserStats");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// 🔋 Get product statistics
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet("products")]
        [ProducesResponseType(typeof(ProductStats), 200)]
        public async Task<IActionResult> GetProductStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var stats = await _dashboardService.GetProductStatsAsync(startDate, endDate);
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProductStats");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// 📦 Get order statistics
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet("orders")]
        [ProducesResponseType(typeof(OrderStats), 200)]
        public async Task<IActionResult> GetOrderStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var stats = await _dashboardService.GetOrderStatsAsync(startDate, endDate);
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrderStats");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// 💰 Get wallet and transaction statistics
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet("wallets")]
        [ProducesResponseType(typeof(WalletStats), 200)]
        public async Task<IActionResult> GetWalletStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var stats = await _dashboardService.GetWalletStatsAsync(startDate, endDate);
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetWalletStats");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// 💵 Get revenue statistics
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet("revenue")]
        [ProducesResponseType(typeof(RevenueStats), 200)]
        public async Task<IActionResult> GetRevenueStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var stats = await _dashboardService.GetRevenueStatsAsync(startDate, endDate);
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRevenueStats");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// 🏆 Get top performers (sellers, buyers, categories)
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet("top-stats")]
        [ProducesResponseType(typeof(TopStatsDto), 200)]
        public async Task<IActionResult> GetTopStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var stats = await _dashboardService.GetTopStatsAsync(startDate, endDate);
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTopStats");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// ⏱️ Get recent activities (orders, products, users)
        /// </summary>
        /// <param name="limit">Number of recent items to return (default: 10)</param>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet("recent-activities")]
        [ProducesResponseType(typeof(RecentActivitiesDto), 200)]
        public async Task<IActionResult> GetRecentActivities(
            [FromQuery] int limit = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                if (limit < 1 || limit > 100)
                    return BadRequest(new { success = false, message = "Limit must be between 1 and 100" });

                var activities = await _dashboardService.GetRecentActivitiesAsync(limit, startDate, endDate);
                return Ok(new { success = true, data = activities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecentActivities");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// � Get orders chart data by month for line chart (all statuses)
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional, default: 12 months ago)</param>
        /// <param name="endDate">End date for filtering (optional, default: now)</param>
        [HttpGet("orders-chart")]
        public async Task<IActionResult> GetOrdersChartData(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var chartData = await _dashboardService.GetOrdersChartDataAsync(startDate, endDate);
                return Ok(new { success = true, data = chartData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrdersChartData");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// 📊 Get revenue chart data by month for line chart (only Delivered orders)
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional, default: 12 months ago)</param>
        /// <param name="endDate">End date for filtering (optional, default: now)</param>
        [HttpGet("revenue-chart")]
        public async Task<IActionResult> GetRevenueChartData(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var chartData = await _dashboardService.GetRevenueChartDataAsync(startDate, endDate);
                return Ok(new { success = true, data = chartData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRevenueChartData");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// 📈 Get order statistics with date range filter
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional, default: 30 days ago)</param>
        /// <param name="endDate">End date for filtering (optional, default: now)</param>
        [HttpGet("orders-by-date")]
        public async Task<IActionResult> GetOrdersByDateRange(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var stats = await _dashboardService.GetOrdersByDateRangeAsync(startDate, endDate);
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrdersByDateRange");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// �🔍 Health check for dashboard service
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new 
            { 
                success = true, 
                service = "Dashboard API", 
                status = "healthy",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
