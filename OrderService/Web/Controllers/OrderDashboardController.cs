using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OrderService.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace OrderService.Web.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class OrderDashboardController : ControllerBase
    {
        private readonly IMongoCollection<Order> _orders;
        private readonly ILogger<OrderDashboardController> _logger;

        public OrderDashboardController(
            IMongoDatabase database,
            ILogger<OrderDashboardController> logger)
        {
            _orders = database.GetCollection<Order>("Orders");
            _logger = logger;
        }

        /// <summary>
        /// 📦 Get order statistics for dashboard
        /// </summary>
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrderStats()
        {
            try
            {
                var allOrders = await _orders.Find(_ => true).ToListAsync();
                var now = DateTime.UtcNow;

                // Trend data (last 7 days)
                var last7Days = Enumerable.Range(0, 7)
                    .Select(i => now.Date.AddDays(-i))
                    .Reverse()
                    .ToList();

                var trendData = last7Days.Select(date =>
                {
                    var dayOrders = allOrders.Where(o => o.CreatedAt.Date == date).ToList();
                    return new
                    {
                        Date = date.ToString("yyyy-MM-dd"),
                        Count = dayOrders.Count,
                        Amount = dayOrders.Sum(o => o.TotalAmount)
                    };
                }).ToList();

                var stats = new
                {
                    TotalOrders = allOrders.Count,
                    PendingOrders = allOrders.Count(o => o.Status == OrderStatus.Pending),
                    ProcessingOrders = allOrders.Count(o => o.Status == OrderStatus.Processing),
                    CompletedOrders = allOrders.Count(o => o.Status == OrderStatus.Delivered),
                    CancelledOrders = allOrders.Count(o => o.Status == OrderStatus.Cancelled),
                    TotalOrderValue = allOrders.Sum(o => o.TotalAmount),
                    AverageOrderValue = allOrders.Any() ? allOrders.Average(o => o.TotalAmount) : 0,
                    TrendData = trendData
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order stats");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 💵 Get revenue statistics
        /// </summary>
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueStats()
        {
            try
            {
                var allOrders = await _orders.Find(_ => true).ToListAsync();
                var completedOrders = allOrders.Where(o => o.Status == OrderStatus.Delivered).ToList();
                
                var now = DateTime.UtcNow;
                var todayStart = now.Date;
                var weekStart = now.AddDays(-7);
                var monthStart = now.AddMonths(-1);
                var yearStart = new DateTime(now.Year, 1, 1);

                // Chart data (last 30 days)
                var last30Days = Enumerable.Range(0, 30)
                    .Select(i => now.Date.AddDays(-i))
                    .Reverse()
                    .ToList();

                var chartData = last30Days.Select(date =>
                {
                    var dayOrders = completedOrders.Where(o => o.CreatedAt.Date == date).ToList();
                    return new
                    {
                        Period = date.ToString("yyyy-MM-dd"),
                        Revenue = dayOrders.Sum(o => o.TotalAmount),
                        OrderCount = dayOrders.Count
                    };
                }).ToList();

                var stats = new
                {
                    TodayRevenue = completedOrders.Where(o => o.CreatedAt >= todayStart).Sum(o => o.TotalAmount),
                    WeekRevenue = completedOrders.Where(o => o.CreatedAt >= weekStart).Sum(o => o.TotalAmount),
                    MonthRevenue = completedOrders.Where(o => o.CreatedAt >= monthStart).Sum(o => o.TotalAmount),
                    YearRevenue = completedOrders.Where(o => o.CreatedAt >= yearStart).Sum(o => o.TotalAmount),
                    TotalRevenue = completedOrders.Sum(o => o.TotalAmount),
                    ChartData = chartData
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue stats");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 🏆 Get top performers statistics
        /// </summary>
        [HttpGet("top-stats")]
        public async Task<IActionResult> GetTopStats()
        {
            try
            {
                var allOrders = await _orders
                    .Find(o => o.Status == OrderStatus.Delivered)
                    .ToListAsync();

                // Top sellers
                var topSellers = allOrders
                    .GroupBy(o => o.Seller.Id)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        Email = g.First().Seller.Email,
                        ProductsSold = g.Count(),
                        TotalRevenue = g.Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .Take(10)
                    .ToList();

                // Top buyers
                var topBuyers = allOrders
                    .GroupBy(o => o.Buyer.Id)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        Email = g.First().Buyer.Email,
                        OrdersCount = g.Count(),
                        TotalSpent = g.Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .Take(10)
                    .ToList();

                // Top categories (by product type)
                var topCategories = allOrders
                    .GroupBy(o => o.Product.Type)
                    .Select(g => new
                    {
                        Category = g.Key,
                        ProductCount = g.Count(),
                        SalesCount = g.Count()
                    })
                    .OrderByDescending(x => x.SalesCount)
                    .Take(10)
                    .ToList();

                var stats = new
                {
                    TopSellers = topSellers,
                    TopBuyers = topBuyers,
                    TopCategories = topCategories
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top stats");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// ⏱️ Get recent orders
        /// </summary>
        [HttpGet("recent-orders")]
        public async Task<IActionResult> GetRecentOrders([FromQuery] int limit = 10)
        {
            try
            {
                var recentOrders = await _orders
                    .Find(_ => true)
                    .SortByDescending(o => o.CreatedAt)
                    .Limit(limit)
                    .ToListAsync();

                var result = recentOrders.Select(o => new
                {
                    Id = o.Id.ToString(),
                    BuyerId = o.Buyer.Id,
                    ProductName = o.Product.Name,
                    Amount = o.TotalAmount,
                    Status = o.Status.ToString(),
                    o.CreatedAt
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent orders");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 📊 Get revenue chart data by month for line chart (only Delivered orders)
        /// </summary>
        [HttpGet("revenue-chart")]
        public async Task<IActionResult> GetRevenueChartData(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Default: last 12 months if no dates provided
                var start = startDate ?? DateTime.UtcNow.AddMonths(-12);
                var end = endDate ?? DateTime.UtcNow;

                // Get completed orders in date range
                var orders = await _orders
                    .Find(o => o.Status == OrderStatus.Delivered && 
                              o.CreatedAt >= start && 
                              o.CreatedAt <= end)
                    .ToListAsync();

                // Group by month and calculate revenue
                var monthlyData = orders
                    .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Revenue = g.Sum(o => o.TotalAmount),
                        OrderCount = g.Count()
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToList();

                // Format for chart: labels and data points
                var chartData = new
                {
                    Labels = monthlyData.Select(m => $"{m.Month:D2}/{m.Year}").ToList(),
                    Revenue = monthlyData.Select(m => m.Revenue).ToList(),
                    OrderCounts = monthlyData.Select(m => m.OrderCount).ToList(),
                    TotalRevenue = monthlyData.Sum(m => m.Revenue),
                    TotalOrders = monthlyData.Sum(m => m.OrderCount),
                    StartDate = start,
                    EndDate = end
                };

                return Ok(chartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue chart data");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 📊 Get all orders chart data by month for line chart (all statuses)
        /// </summary>
        [HttpGet("orders-chart")]
        public async Task<IActionResult> GetOrdersChartData(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Default: last 12 months if no dates provided
                var start = startDate ?? DateTime.UtcNow.AddMonths(-12);
                var end = endDate ?? DateTime.UtcNow;

                // Get all orders in date range
                var orders = await _orders
                    .Find(o => o.CreatedAt >= start && o.CreatedAt <= end)
                    .ToListAsync();

                // Group by month
                var monthlyData = orders
                    .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        TotalAmount = g.Sum(o => o.TotalAmount),
                        OrderCount = g.Count(),
                        DeliveredOrders = g.Count(o => o.Status == OrderStatus.Delivered),
                        DeliveredRevenue = g.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount),
                        PendingOrders = g.Count(o => o.Status == OrderStatus.Pending),
                        ProcessingOrders = g.Count(o => o.Status == OrderStatus.Processing),
                        CancelledOrders = g.Count(o => o.Status == OrderStatus.Cancelled)
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToList();

                // Format for chart
                var chartData = new
                {
                    Labels = monthlyData.Select(m => $"Tháng {m.Month}/{m.Year}").ToList(),
                    Datasets = new
                    {
                        TotalOrders = monthlyData.Select(m => m.OrderCount).ToList(),
                        TotalAmount = monthlyData.Select(m => m.TotalAmount).ToList(),
                        DeliveredOrders = monthlyData.Select(m => m.DeliveredOrders).ToList(),
                        DeliveredRevenue = monthlyData.Select(m => m.DeliveredRevenue).ToList(),
                        PendingOrders = monthlyData.Select(m => m.PendingOrders).ToList(),
                        ProcessingOrders = monthlyData.Select(m => m.ProcessingOrders).ToList(),
                        CancelledOrders = monthlyData.Select(m => m.CancelledOrders).ToList()
                    },
                    Summary = new
                    {
                        TotalOrders = orders.Count,
                        TotalAmount = orders.Sum(o => o.TotalAmount),
                        DeliveredOrders = orders.Count(o => o.Status == OrderStatus.Delivered),
                        DeliveredRevenue = orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount)
                    },
                    DateRange = new
                    {
                        StartDate = start,
                        EndDate = end
                    }
                };

                return Ok(chartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders chart data");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 📈 Get order statistics with date range filter
        /// </summary>
        [HttpGet("orders-by-date")]
        public async Task<IActionResult> GetOrdersByDateRange(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Default: last 30 days if no dates provided
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var orders = await _orders
                    .Find(o => o.CreatedAt >= start && o.CreatedAt <= end)
                    .ToListAsync();

                var stats = new
                {
                    DateRange = new
                    {
                        StartDate = start,
                        EndDate = end
                    },
                    TotalOrders = orders.Count,
                    OrdersByStatus = new
                    {
                        Pending = orders.Count(o => o.Status == OrderStatus.Pending),
                        Processing = orders.Count(o => o.Status == OrderStatus.Processing),
                        Delivered = orders.Count(o => o.Status == OrderStatus.Delivered),
                        Cancelled = orders.Count(o => o.Status == OrderStatus.Cancelled)
                    },
                    TotalRevenue = orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount),
                    AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
                    Orders = orders.Select(o => new
                    {
                        Id = o.Id.ToString(),
                        BuyerEmail = o.Buyer.Email,
                        SellerEmail = o.Seller.Email,
                        ProductName = o.Product.Name,
                        Amount = o.TotalAmount,
                        Status = o.Status.ToString(),
                        CreatedAt = o.CreatedAt
                    })
                    .OrderByDescending(o => o.CreatedAt)
                    .ToList()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders by date range");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// 🔍 Health check
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new 
            { 
                success = true, 
                service = "OrderService Dashboard", 
                status = "healthy",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
