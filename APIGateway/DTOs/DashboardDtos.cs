namespace APIGateway.DTOs
{
    // ============ OVERVIEW STATS ============
    public class DashboardOverviewDto
    {
        public UserStats Users { get; set; } = new();
        public ProductStats Products { get; set; } = new();
        public OrderStats Orders { get; set; } = new();
        public WalletStats Wallets { get; set; } = new();
        public RevenueStats Revenue { get; set; } = new();
    }

    // ============ USER STATISTICS ============
    public class UserStats
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int NewUsersThisMonth { get; set; }
        public List<UserGrowthDto> GrowthData { get; set; } = new();
    }

    public class UserGrowthDto
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    // ============ PRODUCT STATISTICS ============
    public class ProductStats
    {
        public int TotalProducts { get; set; }
        public int PublishedProducts { get; set; }
        public int PendingProducts { get; set; }
        public int SoldProducts { get; set; }
        public int DraftProducts { get; set; }
        public int RejectedProducts { get; set; }
        public List<ProductStatusDto> StatusBreakdown { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
    }

    public class ProductStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class TopProductDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int ViewCount { get; set; }
    }

    // ============ ORDER STATISTICS ============
    public class OrderStats
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalOrderValue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<OrderTrendDto> TrendData { get; set; } = new();
    }

    public class OrderTrendDto
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    // ============ WALLET STATISTICS ============
    public class WalletStats
    {
        public int TotalWallets { get; set; }
        public decimal TotalBalance { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalDeposits { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public int TodayTransactions { get; set; }
        public List<TransactionTrendDto> TransactionTrends { get; set; } = new();
    }

    public class TransactionTrendDto
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    // ============ REVENUE STATISTICS ============
    public class RevenueStats
    {
        public decimal TodayRevenue { get; set; }
        public decimal WeekRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal YearRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<RevenueChartDto> ChartData { get; set; } = new();
    }

    public class RevenueChartDto
    {
        public string Period { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    // ============ TOP STATISTICS ============
    public class TopStatsDto
    {
        public List<TopSellerDto> TopSellers { get; set; } = new();
        public List<TopBuyerDto> TopBuyers { get; set; } = new();
        public List<TopProductCategoryDto> TopCategories { get; set; } = new();
    }

    public class TopSellerDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int ProductsSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class TopBuyerDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class TopProductCategoryDto
    {
        public string Category { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int SalesCount { get; set; }
    }

    // ============ RECENT ACTIVITIES ============
    public class RecentActivitiesDto
    {
        public List<RecentOrderDto> RecentOrders { get; set; } = new();
        public List<RecentProductDto> RecentProducts { get; set; } = new();
        public List<RecentUserDto> RecentUsers { get; set; } = new();
    }

    public class RecentOrderDto
    {
        public string Id { get; set; } = string.Empty;
        public string BuyerId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class RecentProductDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class RecentUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
