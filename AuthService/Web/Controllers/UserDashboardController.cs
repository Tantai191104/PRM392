using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using AuthService.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AuthService.Web.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class UserDashboardController : ControllerBase
    {
        private readonly IMongoCollection<User> _users;
        private readonly ILogger<UserDashboardController> _logger;

        public UserDashboardController(
            IMongoDatabase database,
            ILogger<UserDashboardController> logger)
        {
            _users = database.GetCollection<User>("Users");
            _logger = logger;
        }

        /// <summary>
        /// 👥 Get user statistics for dashboard
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                var allUsers = await _users.Find(_ => true).ToListAsync();
                var now = DateTime.UtcNow;
                
                var todayStart = now.Date;
                var weekStart = now.AddDays(-7);
                var monthStart = now.AddMonths(-1);

                // Growth data (last 7 days)
                var last7Days = Enumerable.Range(0, 7)
                    .Select(i => now.Date.AddDays(-i))
                    .Reverse()
                    .ToList();

                var growthData = last7Days.Select(date => new
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Count = allUsers.Count(u => u.CreatedAt.Date == date)
                }).ToList();

                var stats = new
                {
                    TotalUsers = allUsers.Count,
                    ActiveUsers = allUsers.Count(u => u.IsActive),
                    NewUsersToday = allUsers.Count(u => u.CreatedAt >= todayStart),
                    NewUsersThisWeek = allUsers.Count(u => u.CreatedAt >= weekStart),
                    NewUsersThisMonth = allUsers.Count(u => u.CreatedAt >= monthStart),
                    GrowthData = growthData
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// ⏱️ Get recent users
        /// </summary>
        [HttpGet("recent-users")]
        public async Task<IActionResult> GetRecentUsers([FromQuery] int limit = 10)
        {
            try
            {
                var recentUsers = await _users
                    .Find(_ => true)
                    .SortByDescending(u => u.CreatedAt)
                    .Limit(limit)
                    .ToListAsync();

                var result = recentUsers.Select(u => new
                {
                    Id = u.Id.ToString(),
                    u.Email,
                    Role = u.Role.ToString(),
                    u.CreatedAt
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent users");
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
                service = "AuthService Dashboard", 
                status = "healthy",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
