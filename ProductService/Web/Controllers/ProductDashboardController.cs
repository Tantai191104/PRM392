using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ProductService.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ProductService.Web.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class ProductDashboardController : ControllerBase
    {
        private readonly IMongoCollection<Product> _products;
        private readonly ILogger<ProductDashboardController> _logger;

        public ProductDashboardController(
            IMongoDatabase database,
            ILogger<ProductDashboardController> logger)
        {
            _products = database.GetCollection<Product>("Products");
            _logger = logger;
        }

        /// <summary>
        /// 🔋 Get product statistics for dashboard
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> GetProductStats()
        {
            try
            {
                var allProducts = await _products.Find(_ => true).ToListAsync();
                var total = allProducts.Count;

                var statusBreakdown = allProducts
                    .GroupBy(p => p.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Percentage = total > 0 ? Math.Round((decimal)g.Count() / total * 100, 2) : 0
                    })
                    .ToList();

                // Top products (by price)
                var topProducts = allProducts
                    .Where(p => p.Status == "Published")
                    .OrderByDescending(p => p.Price)
                    .Take(10)
                    .Select(p => new
                    {
                        Id = p.Id.ToString(),
                        p.Name,
                        p.Price,
                        ViewCount = 0 // Add view tracking if needed
                    })
                    .ToList();

                var stats = new
                {
                    TotalProducts = total,
                    PublishedProducts = allProducts.Count(p => p.Status == "Published"),
                    PendingProducts = allProducts.Count(p => p.Status == "Pending"),
                    SoldProducts = allProducts.Count(p => p.Status == "Sold"),
                    DraftProducts = allProducts.Count(p => p.Status == "Draft"),
                    RejectedProducts = allProducts.Count(p => p.Status == "Rejected"),
                    StatusBreakdown = statusBreakdown,
                    TopProducts = topProducts
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product stats");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// ⏱️ Get recent products
        /// </summary>
        [HttpGet("recent-products")]
        public async Task<IActionResult> GetRecentProducts([FromQuery] int limit = 10)
        {
            try
            {
                var recentProducts = await _products
                    .Find(_ => true)
                    .SortByDescending(p => p.CreatedAt)
                    .Limit(limit)
                    .ToListAsync();

                var result = recentProducts.Select(p => new
                {
                    Id = p.Id.ToString(),
                    p.Name,
                    p.Price,
                    Status = p.Status,
                    p.CreatedAt
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent products");
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
                service = "ProductService Dashboard", 
                status = "healthy",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
