# 📊 HƯỚNG DẪN TRIỂN KHAI DASHBOARD APIs

## 🎯 TỔNG QUAN

Dashboard API đã được tạo ở **APIGateway** để tổng hợp thống kê từ tất cả services:

### ✅ ĐÃ HOÀN THÀNH
- ✅ APIGateway/Controllers/DashboardController.cs - Main dashboard controller
- ✅ APIGateway/Services/DashboardService.cs - Aggregation service
- ✅ APIGateway/DTOs/DashboardDtos.cs - All response models
- ✅ WalletService - Dashboard endpoints hoàn chỉnh

### 📋 CẦN TRIỂN KHAI

Các services còn lại cần thêm dashboard endpoints:

## 1️⃣ AuthService - User Statistics

Tạo file: `AuthService/Web/Controllers/UserDashboardController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Repositories;

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
                    .Reverse();

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
    }
}
```

## 2️⃣ ProductService - Product Statistics

Tạo file: `ProductService/Web/Controllers/ProductDashboardController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ProductService.Domain;
using ProductService.Domain.Enums;

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
                        Status = g.Key.ToString(),
                        Count = g.Count(),
                        Percentage = total > 0 ? (decimal)g.Count() / total * 100 : 0
                    })
                    .ToList();

                // Top products (by capacity or price)
                var topProducts = allProducts
                    .Where(p => p.Status == ProductStatus.Published)
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
                    PublishedProducts = allProducts.Count(p => p.Status == ProductStatus.Published),
                    PendingProducts = allProducts.Count(p => p.Status == ProductStatus.Pending),
                    SoldProducts = allProducts.Count(p => p.Status == ProductStatus.Sold),
                    DraftProducts = allProducts.Count(p => p.Status == ProductStatus.Draft),
                    RejectedProducts = allProducts.Count(p => p.Status == ProductStatus.Rejected),
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
                    Status = p.Status.ToString(),
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
    }
}
```

## 3️⃣ OrderService - Order & Revenue Statistics

Tạo file: `OrderService/Web/Controllers/OrderDashboardController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;

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
                    .Reverse();

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
                    CompletedOrders = allOrders.Count(o => o.Status == OrderStatus.Completed),
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

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueStats()
        {
            try
            {
                var allOrders = await _orders.Find(_ => true).ToListAsync();
                var completedOrders = allOrders.Where(o => o.Status == OrderStatus.Completed).ToList();
                
                var now = DateTime.UtcNow;
                var todayStart = now.Date;
                var weekStart = now.AddDays(-7);
                var monthStart = now.AddMonths(-1);
                var yearStart = new DateTime(now.Year, 1, 1);

                // Chart data (last 30 days)
                var last30Days = Enumerable.Range(0, 30)
                    .Select(i => now.Date.AddDays(-i))
                    .Reverse();

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

        [HttpGet("top-stats")]
        public async Task<IActionResult> GetTopStats()
        {
            try
            {
                var allOrders = await _orders
                    .Find(o => o.Status == OrderStatus.Completed)
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
    }
}
```

## 🚀 CÁCH TRIỂN KHAI

### Bước 1: Thêm Dashboard Controllers

Copy các controller code ở trên vào từng service:
- AuthService → UserDashboardController.cs
- ProductService → ProductDashboardController.cs  
- OrderService → OrderDashboardController.cs

### Bước 2: Cập nhật Program.cs trong APIGateway

```csharp
// Add DashboardService
builder.Services.AddHttpClient<DashboardService>();
builder.Services.AddScoped<DashboardService>();
```

### Bước 3: Cấu hình Service URLs

Thêm vào `appsettings.json` của APIGateway:

```json
{
  "Services": {
    "AuthService": "http://authservice:8080",
    "ProductService": "http://productservice:8080",
    "OrderService": "http://orderservice:8080",
    "WalletService": "http://walletservice:8080",
    "EscrowService": "http://escrowservice:8080",
    "ChatService": "http://chatservice:8080"
  }
}
```

### Bước 4: Build và Test

```bash
# Build tất cả services
docker compose build

# Start services
docker compose up -d

# Test Dashboard API
curl http://localhost:8080/api/dashboard/overview
curl http://localhost:8080/api/dashboard/users
curl http://localhost:8080/api/dashboard/products
curl http://localhost:8080/api/dashboard/orders
curl http://localhost:8080/api/dashboard/wallets
curl http://localhost:8080/api/dashboard/revenue
```

## 📊 DASHBOARD ENDPOINTS

### APIGateway (Main)
- `GET /api/dashboard/overview` - Tổng quan toàn bộ hệ thống
- `GET /api/dashboard/users` - Thống kê users
- `GET /api/dashboard/products` - Thống kê products
- `GET /api/dashboard/orders` - Thống kê orders
- `GET /api/dashboard/wallets` - Thống kê wallets
- `GET /api/dashboard/revenue` - Thống kê doanh thu
- `GET /api/dashboard/top-stats` - Top performers
- `GET /api/dashboard/recent-activities?limit=10` - Hoạt động gần đây

### Individual Services (Optional direct access)
- AuthService: `/api/dashboard/users`, `/api/dashboard/recent-users`
- ProductService: `/api/dashboard/products`, `/api/dashboard/recent-products`
- OrderService: `/api/dashboard/orders`, `/api/dashboard/revenue`, `/api/dashboard/top-stats`
- WalletService: `/api/dashboard/wallets`, `/api/dashboard/transactions`, `/api/dashboard/top-wallets`

## 🎨 FRONTEND INTEGRATION

Example React/Angular code:

```javascript
// Get overview
const response = await fetch('http://localhost:8080/api/dashboard/overview');
const data = await response.json();

console.log(data.data.users.totalUsers);
console.log(data.data.products.totalProducts);
console.log(data.data.revenue.todayRevenue);
```

## ⚡ OPTIMIZATION TIP

Thêm caching cho dashboard data trong APIGateway:

```csharp
builder.Services.AddMemoryCache();

// In DashboardService
private readonly IMemoryCache _cache;

public async Task<DashboardOverviewDto> GetOverviewAsync()
{
    return await _cache.GetOrCreateAsync("dashboard:overview", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        // ... fetch data
    });
}
```

## ✅ CHECKLIST

- [ ] Tạo UserDashboardController trong AuthService
- [ ] Tạo ProductDashboardController trong ProductService
- [ ] Tạo OrderDashboardController trong OrderService
- [ ] Cập nhật Program.cs trong APIGateway
- [ ] Cấu hình service URLs trong appsettings.json
- [ ] Build và test tất cả endpoints
- [ ] Thêm caching nếu cần
- [ ] Tạo frontend dashboard UI

## 🐛 TROUBLESHOOTING

**Lỗi: Service không kết nối được**
→ Kiểm tra docker-compose.yml có đúng service names
→ Kiểm tra ports đã expose chưa

**Lỗi: Null reference**
→ Đảm bảo MongoDB collections đã có data
→ Thêm null checks trong code

**Lỗi: Slow performance**
→ Thêm indexes vào MongoDB
→ Implement caching
→ Giảm số lượng aggregation queries

---

**Tạo bởi:** Dashboard API Generator  
**Ngày:** 2024  
**Version:** 1.0
