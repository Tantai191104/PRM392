# 🔒 Hướng Dẫn Redis Cache & Rate Limiting - PRM392

## 📋 Mục Lục
1. [Tổng Quan Kiến Trúc](#tổng-quan-kiến-trúc)
2. [Redis Cache](#redis-cache)
3. [Rate Limiting](#rate-limiting)
4. [Cấu Hình](#cấu-hình)
5. [Sử Dụng](#sử-dụng)
6. [Monitoring & Testing](#monitoring--testing)

---

## 🏗️ Tổng Quan Kiến Trúc

### Các Component Đã Thêm

```
┌─────────────────────────────────────────────────────────────┐
│                         Client                              │
└─────────────────┬───────────────────────────────────────────┘
                  │ HTTP Requests
                  ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  1. Rate Limiting Middleware                         │  │
│  │     ✓ Kiểm tra số request từ IP                      │  │
│  │     ✓ Redis: rate_limit:{IP} → count                 │  │
│  └──────────────────────────────────────────────────────┘  │
│                        │                                    │
│                        ▼                                    │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  2. Cache Service                                    │  │
│  │     ✓ Check cache trước khi gọi service             │  │
│  │     ✓ Redis: cache:{key} → data                     │  │
│  └──────────────────────────────────────────────────────┘  │
│                        │                                    │
│                        ▼                                    │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  3. Downstream Services                              │  │
│  │     • AuthService                                    │  │
│  │     • ProductService                                 │  │
│  │     • OrderService                                   │  │
│  │     • WalletService                                  │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│                    Redis Server                              │
│  Port: 6379                                                 │
│  Password: PRM392Redis2024!SecurePassword                   │
└─────────────────────────────────────────────────────────────┘
```

---

## 🚀 Redis Cache

### 📍 Cache Ở Đâu?

#### 1. **CacheService.cs** - Service chính để cache
**Vị trí:** `APIGateway/Services/CacheService.cs`

```csharp
// Các methods chính:
- GetAsync<T>(key)           // Lấy data từ cache
- SetAsync<T>(key, value)    // Lưu data vào cache
- GetOrCreateAsync<T>()      // Lấy từ cache hoặc tạo mới
- RemoveAsync(key)           // Xóa cache
```

**Cách sử dụng:**
```csharp
// Example: Cache user data
var user = await _cacheService.GetOrCreateAsync(
    key: "user:123",
    factory: async () => await _userService.GetUser(123),
    expiration: TimeSpan.FromMinutes(10)
);
```

#### 2. **DashboardService.cs** - Sử dụng cache
**Vị trí:** `APIGateway/Services/DashboardService.cs`

**Đã tích hợp CacheService:**
```csharp
public class DashboardService
{
    private readonly CacheService _cacheService;
    
    public DashboardService(..., CacheService cacheService)
    {
        _cacheService = cacheService;
    }
}
```

**Ví dụ cache dashboard data:**
```csharp
// Trong GetOverviewAsync() - có thể thêm cache như này:
public async Task<DashboardOverviewDto> GetOverviewAsync(DateTime? startDate = null, DateTime? endDate = null)
{
    var cacheKey = $"dashboard:overview:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}";
    
    return await _cacheService.GetOrCreateAsync(
        cacheKey,
        async () => {
            // Logic gốc để fetch data
            var overview = new DashboardOverviewDto();
            // ... fetch from services
            return overview;
        },
        TimeSpan.FromMinutes(2) // Cache 2 phút
    );
}
```

### 📦 Redis Keys Structure

```
PRM392:rate_limit:{IP_ADDRESS}          → Request count (TTL: 1 minute)
PRM392:cache:dashboard:overview:*       → Dashboard overview data (TTL: 2 min)
PRM392:cache:user:{userId}              → User data (TTL: 10 min)
PRM392:cache:product:{productId}        → Product data (TTL: 5 min)
PRM392:cache:orders:chart:*             → Orders chart data (TTL: 5 min)
```

### ⚙️ Cache Configuration

**File:** `APIGateway/appsettings.json`

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379,password=PRM392Redis2024!SecurePassword",
    "InstanceName": "PRM392:"
  },
  "CacheSettings": {
    "DefaultExpirationMinutes": 5,
    "DashboardCacheMinutes": 2,
    "UserCacheMinutes": 10,
    "ProductCacheMinutes": 5
  }
}
```

### 🔧 Docker Configuration

**File:** `docker-compose.yml`

```yaml
redis:
  image: redis:7-alpine
  container_name: prm392_redis
  restart: always
  ports:
    - "6379:6379"
  command: redis-server --appendonly yes --requirepass "PRM392Redis2024!SecurePassword"
  volumes:
    - redis_data:/data
  networks:
    - prm392_network
```

---

## 🛡️ Rate Limiting

### 📍 Rate Limiting Ở Đâu?

#### 1. **RateLimitingMiddleware.cs** - Middleware chính
**Vị trí:** `APIGateway/Middleware/RateLimitingMiddleware.cs`

**Cơ chế hoạt động:**
```csharp
1. Lấy IP address từ request
2. Tạo key: rate_limit:{IP}
3. Check count từ Redis
4. Nếu >= limit → Return 429 (Too Many Requests)
5. Nếu < limit → Tăng counter và cho phép request
```

**Code logic:**
```csharp
public async Task InvokeAsync(HttpContext context)
{
    var ipAddress = GetClientIpAddress(context);
    var key = $"rate_limit:{ipAddress}";
    
    var currentCount = await GetRequestCountAsync(key);
    
    if (currentCount >= _requestLimit) // 100 requests/minute
    {
        // BLOCK REQUEST
        context.Response.StatusCode = 429;
        await context.Response.WriteAsJsonAsync(new {
            success = false,
            message = "Too many requests. Please try again later.",
            retryAfter = 60
        });
        return;
    }
    
    // ALLOW REQUEST
    await IncrementRequestCountAsync(key);
    await _next(context);
}
```

#### 2. **GetClientIpAddress()** - Lấy IP từ request

```csharp
private string GetClientIpAddress(HttpContext context)
{
    // 1. Check X-Forwarded-For (từ proxy/load balancer)
    var forwardedFor = context.Request.Headers["X-Forwarded-For"];
    
    // 2. Check X-Real-IP
    var realIp = context.Request.Headers["X-Real-IP"];
    
    // 3. Fallback: RemoteIpAddress
    return context.Connection.RemoteIpAddress?.ToString();
}
```

#### 3. **Program.cs** - Đăng ký middleware
**Vị trí:** `APIGateway/Program.cs`

```csharp
// Thêm Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379,password=...";
    options.InstanceName = "PRM392:";
});

// Đăng ký middleware
var enableRateLimiting = builder.Configuration.GetValue<bool>("RateLimit:EnableRateLimiting", true);
if (enableRateLimiting)
{
    app.UseMiddleware<RateLimitingMiddleware>();
}
```

### ⚙️ Rate Limiting Configuration

**File:** `APIGateway/appsettings.json`

```json
{
  "RateLimit": {
    "RequestLimit": 100,           // Số request tối đa
    "TimeWindowMinutes": 1,        // Trong 1 phút
    "EnableRateLimiting": true     // Bật/tắt rate limiting
  }
}
```

**Các giá trị khuyến nghị:**

| Use Case | RequestLimit | TimeWindow | Mô tả |
|----------|--------------|------------|-------|
| Development | 1000 | 1 min | Không giới hạn nhiều |
| Normal API | 100 | 1 min | User thông thường |
| Public API | 60 | 1 min | API công khai |
| Strict | 30 | 1 min | API nhạy cảm |
| Dashboard | 50 | 1 min | Cho dashboard |

### 📊 Response Headers

Khi request thành công, API trả về các headers:

```http
HTTP/1.1 200 OK
X-Rate-Limit-Limit: 100
X-Rate-Limit-Remaining: 95
X-Rate-Limit-Reset: 1699999999
```

Khi bị block (429):

```http
HTTP/1.1 429 Too Many Requests
X-Rate-Limit-Limit: 100
X-Rate-Limit-Remaining: 0
X-Rate-Limit-Reset: 1699999999

{
  "success": false,
  "message": "Too many requests. Please try again later.",
  "retryAfter": 60
}
```

---

## 🔧 Cấu Hình Chi Tiết

### 1. Environment Variables

**Docker Compose:**
```yaml
apigateway:
  environment:
    Redis__ConnectionString: "prm392_redis:6379,password=PRM392Redis2024!SecurePassword"
    RateLimit__RequestLimit: 100
    RateLimit__TimeWindowMinutes: 1
    RateLimit__EnableRateLimiting: true
```

### 2. appsettings.json

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379,password=PRM392Redis2024!SecurePassword",
    "InstanceName": "PRM392:"
  },
  "RateLimit": {
    "RequestLimit": 100,
    "TimeWindowMinutes": 1,
    "EnableRateLimiting": true
  },
  "CacheSettings": {
    "DefaultExpirationMinutes": 5,
    "DashboardCacheMinutes": 2,
    "UserCacheMinutes": 10,
    "ProductCacheMinutes": 5
  }
}
```

### 3. NuGet Packages

**File:** `APIGateway/APIGateway.csproj`

```xml
<PackageReference Include="StackExchange.Redis" Version="2.8.16" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
<PackageReference Include="AspNetCoreRateLimit" Version="5.0.0" />
```

---

## 💻 Sử Dụng

### Cache Usage Example

#### 1. Inject CacheService

```csharp
public class YourService
{
    private readonly CacheService _cacheService;
    
    public YourService(CacheService cacheService)
    {
        _cacheService = cacheService;
    }
}
```

#### 2. Sử dụng trong method

```csharp
// Pattern 1: Get or Create
public async Task<ProductDto> GetProduct(string id)
{
    return await _cacheService.GetOrCreateAsync(
        key: $"product:{id}",
        factory: async () => await _productService.GetProductById(id),
        expiration: TimeSpan.FromMinutes(5)
    );
}

// Pattern 2: Manual cache
public async Task<UserDto> GetUser(string id)
{
    // Check cache first
    var cached = await _cacheService.GetAsync<UserDto>($"user:{id}");
    if (cached != null) return cached;
    
    // Fetch from DB
    var user = await _userService.GetUserById(id);
    
    // Save to cache
    await _cacheService.SetAsync($"user:{id}", user, TimeSpan.FromMinutes(10));
    
    return user;
}

// Pattern 3: Invalidate cache
public async Task UpdateProduct(string id, ProductDto product)
{
    await _productService.Update(id, product);
    
    // Remove cache
    await _cacheService.RemoveAsync($"product:{id}");
}
```

### Rate Limiting Usage

**Rate limiting tự động chạy cho TẤT CẢ requests qua APIGateway!**

Không cần code gì thêm, middleware tự động:
- ✅ Check mọi request
- ✅ Track theo IP address
- ✅ Return 429 nếu vượt limit
- ✅ Add headers vào response

**Bypass rate limiting cho endpoint cụ thể:**

```csharp
// Trong RateLimitingMiddleware.cs
public async Task InvokeAsync(HttpContext context)
{
    // Whitelist endpoints không cần rate limit
    var path = context.Request.Path.Value;
    if (path?.StartsWith("/health") == true || 
        path?.StartsWith("/swagger") == true)
    {
        await _next(context);
        return;
    }
    
    // Continue with rate limiting...
}
```

---

## 📊 Monitoring & Testing

### 1. Test Rate Limiting

#### Test với curl (5 requests liên tục):

```bash
for i in {1..5}; do 
  echo "Request $i:"
  curl -s -w "\nStatus: %{http_code}\n" \
    -H "X-Forwarded-For: 192.168.1.100" \
    "http://localhost:5000/api/dashboard/health"
  echo "---"
done
```

#### Test vượt limit (105 requests):

```bash
for i in {1..105}; do 
  curl -s -w "%{http_code} " \
    "http://localhost:5000/api/dashboard/overview"
done
```

**Kết quả mong đợi:**
- Request 1-100: `200 OK`
- Request 101-105: `429 Too Many Requests`

### 2. Monitor Redis

#### Kết nối vào Redis container:

```bash
docker exec -it prm392_redis redis-cli -a PRM392Redis2024!SecurePassword
```

#### Các commands hữu ích:

```redis
# Xem tất cả keys
KEYS PRM392:*

# Xem rate limit của IP
GET PRM392:rate_limit:192.168.1.100

# Xem TTL (time to live)
TTL PRM392:rate_limit:192.168.1.100

# Xem cache data
GET PRM392:cache:dashboard:overview

# Xem thống kê
INFO stats

# Xem memory usage
INFO memory

# Xóa tất cả cache (CẨN THẬN!)
FLUSHDB

# Monitor real-time commands
MONITOR
```

### 3. Check Redis Container

```bash
# Check container status
docker ps | grep redis

# Check logs
docker logs prm392_redis --tail 50

# Check memory usage
docker stats prm392_redis

# Restart Redis
docker restart prm392_redis
```

### 4. Performance Metrics

#### Xem cache hit rate:

```bash
docker exec -it prm392_redis redis-cli -a PRM392Redis2024!SecurePassword INFO stats | grep keyspace
```

**Output:**
```
keyspace_hits:1500      # Cache hit
keyspace_misses:100     # Cache miss
```

**Cache hit rate = 1500 / (1500 + 100) = 93.75%** ✅ Good!

---

## 🎯 Best Practices

### Cache

1. **Cache key naming convention:**
   ```
   PRM392:cache:{entity}:{id}:{version}
   ```

2. **Set appropriate TTL:**
   - Static data (categories, config): 1 hour+
   - User data: 10-15 minutes
   - Dashboard data: 2-5 minutes
   - Real-time data: Don't cache or 30 seconds

3. **Invalidate cache khi update:**
   ```csharp
   await _cacheService.RemoveAsync($"product:{id}");
   ```

4. **Handle cache failures gracefully:**
   ```csharp
   try {
       var cached = await _cacheService.GetAsync<T>(key);
   } catch {
       // Fallback to direct DB call
       return await _dbService.Get();
   }
   ```

### Rate Limiting

1. **Different limits for different endpoints:**
   - Dashboard: 50 req/min
   - Auth: 20 req/min
   - Public API: 100 req/min

2. **Whitelist trusted IPs:**
   ```csharp
   var trustedIPs = new[] { "10.0.0.1", "192.168.1.100" };
   if (trustedIPs.Contains(ipAddress)) {
       await _next(context);
       return;
   }
   ```

3. **Rate limit per user (not just IP):**
   ```csharp
   var userId = context.User?.FindFirst("userId")?.Value;
   var key = $"rate_limit:user:{userId}";
   ```

---

## 🐛 Troubleshooting

### Redis không connect được

```bash
# Check Redis running
docker ps | grep redis

# Check logs
docker logs prm392_redis

# Test connection
docker exec -it prm392_redis redis-cli -a PRM392Redis2024!SecurePassword PING
# Should return: PONG
```

### Rate limiting không hoạt động

1. Check `EnableRateLimiting` = true trong appsettings.json
2. Check Redis đang chạy
3. Check logs: `docker logs prm392_apigateway`

### Cache không update sau khi modify data

```csharp
// Nhớ xóa cache sau khi update
await _cacheService.RemoveAsync($"product:{id}");
```

---

## 📚 Tài Liệu Tham Khảo

- [Redis Documentation](https://redis.io/documentation)
- [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/)
- [ASP.NET Core Caching](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/)
- [Rate Limiting Patterns](https://cloud.google.com/architecture/rate-limiting-strategies-techniques)

---

## 📞 Support

Có vấn đề? Check:
1. Redis logs: `docker logs prm392_redis`
2. APIGateway logs: `docker logs prm392_apigateway`
3. Redis CLI: `docker exec -it prm392_redis redis-cli -a PRM392Redis2024!SecurePassword`

---

**🎉 Happy Coding with Redis & Rate Limiting!**
