using MongoDB.Driver;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using SharedKernel.Entities;
using Microsoft.Extensions.Configuration;
using OrderService.Infrastructure.ExternalServices;

namespace OrderService.Application.Services
{
    public interface IOrderAppService
    {
        Task<Product?> GetProductForOrderAsync(string productId);
        Task<bool> UpdateProductStatusAsync(string productId, string status);
        Task<OrderResponseDto> CreateOrderAsync(OrderDto dto, string buyerId);
        Task<OrderResponseDto?> GetOrderByIdAsync(string id);
        Task<List<OrderResponseDto>> GetOrdersByBuyerAsync(string buyerId);
        Task<List<OrderResponseDto>> GetOrdersBySellerAsync(string sellerId);
        Task<List<OrderResponseDto>> GetAllOrdersAsync();
        Task<bool> UpdateOrderStatusAsync(string id, OrderStatus status, string updatedById, string updatedBy);
        Task<(List<OrderResponseDto>, int)> GetAllOrdersPagedAsync(int page, int pageSize);
        Task<bool> CancelOrderAsync(string id, string userId, string userName);
    }

    public class OrderAppService : IOrderAppService
    {
        private readonly IMongoCollection<Order> _orders;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthService _authService;
        private readonly IProductService _productService;
        private readonly string _productServiceBaseUrl;

        public OrderAppService(
            IMongoDatabase database,
            IHttpClientFactory httpClientFactory,
            IAuthService authService,
            IProductService productService,
            IConfiguration config)
        {
            _orders = database.GetCollection<Order>("Orders");
            _httpClientFactory = httpClientFactory;
            _authService = authService;
            _productService = productService;
            _productServiceBaseUrl = config["ExternalServices:ProductService:BaseUrl"]
                ?? "http://productservice:5137";
        }

        public async Task<Product?> GetProductForOrderAsync(string productId)
        {
            var productDto = await _productService.GetProductAsync(productId);
            if (productDto == null)
                return null;

            // Convert ProductDto to Product entity
            return new Product
            {
                Id = productDto.Id,
                Name = productDto.Name,
                Brand = productDto.Brand,
                Type = productDto.Type,
                Capacity = productDto.Capacity,
                Condition = productDto.Condition,
                Year = productDto.Year,
                Price = productDto.Price,
                Voltage = productDto.Voltage,
                CycleCount = productDto.CycleCount,
                Location = productDto.Location,
                Warranty = productDto.Warranty,
                Status = productDto.Status,
                Images = productDto.Images,
                Description = productDto.Description,
                OwnerId = productDto.OwnerId
            };
        }

        public async Task<bool> UpdateProductStatusAsync(string productId, string status)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{_productServiceBaseUrl}/api/products/{productId}/status";
            var content = new StringContent(
                $"{{\"status\":\"{status}\"}}",
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync(url, content);
            return response.IsSuccessStatusCode;
        }

        private async Task UpdateProductStatusInternalAsync(string productId, string status)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{_productServiceBaseUrl}/api/products/{productId}/status";
            var content = new StringContent(
                $"{{\"status\":\"{status}\"}}",
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync(url, content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<Product> GetProductAsync(string productId)
        {
            var productDto = await _productService.GetProductAsync(productId);
            if (productDto == null)
                throw new HttpRequestException($"Failed to fetch product with ID {productId}");

            // Convert ProductDto to Product entity
            return new Product
            {
                Id = productDto.Id,
                Name = productDto.Name,
                Brand = productDto.Brand,
                Type = productDto.Type,
                Capacity = productDto.Capacity,
                Condition = productDto.Condition,
                Year = productDto.Year,
                Price = productDto.Price,
                Voltage = productDto.Voltage,
                CycleCount = productDto.CycleCount,
                Location = productDto.Location,
                Warranty = productDto.Warranty,
                Status = productDto.Status,
                Images = productDto.Images,
                Description = productDto.Description,
                OwnerId = productDto.OwnerId
            };
        }

        private async Task<User?> GetUserAsync(string userId)
        {
            var userDto = await _authService.GetUserAsync(userId);
            if (userDto == null)
                return null;

            // Convert UserDto to User entity
            // Ưu tiên DisplayName, sau đó FullName, cuối cùng mới là Name
            var userName = !string.IsNullOrWhiteSpace(userDto.DisplayName)
                ? userDto.DisplayName
                : (!string.IsNullOrWhiteSpace(userDto.FullName)
                    ? userDto.FullName
                    : userDto.Name);

            return new User
            {
                Id = userDto.Id,
                Name = userName,
                Email = userDto.Email,
                Phone = userDto.Phone ?? string.Empty
            };
        }

        public async Task<OrderResponseDto> CreateOrderAsync(OrderDto dto, string buyerId)
        {
            var product = await GetProductAsync(dto.ProductId);

            // Lấy thông tin người mua từ AuthService
            var buyer = await GetUserAsync(buyerId);
            if (buyer == null)
                throw new InvalidOperationException("Buyer information not found");

            // Lấy thông tin người bán từ AuthService
            var seller = await GetUserAsync(product.OwnerId);
            if (seller == null)
                throw new InvalidOperationException("Seller information not found");

            // Tính tổng tiền = giá sản phẩm + phí ship
            var totalAmount = product.Price + dto.ShippingFee;

            var order = new Order
            {
                Buyer = buyer,
                Seller = seller,
                Product = product,
                TotalAmount = totalAmount,
                ShippingFee = dto.ShippingFee,
                PaymentMethod = dto.PaymentMethod,
                ShippingAddress = dto.ShippingAddress,
                Notes = dto.Notes,
                Status = OrderStatus.Pending,
                Timeline = new List<OrderTimelineEntry>
                {
                    new()
                    {
                        FromStatus = "None",
                        ToStatus = "Pending",
                        UpdatedById = buyerId,
                        UpdatedBy = buyer.Name,
                        UpdatedAt = DateTime.UtcNow
                    }
                }
            };

            await _orders.InsertOneAsync(order);
            await UpdateProductStatusInternalAsync(dto.ProductId, "InTransaction");

            return ToResponseDto(order);
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(string id)
        {
            var order = await _orders.Find(x => x.Id == id).FirstOrDefaultAsync();
            return order == null ? null : ToResponseDto(order);
        }

        public async Task<List<OrderResponseDto>> GetOrdersByBuyerAsync(string buyerId)
        {
            var orders = await _orders.Find(x => x.Buyer.Id == buyerId).ToListAsync();
            return orders.Select(ToResponseDto).ToList();
        }

        public async Task<List<OrderResponseDto>> GetOrdersBySellerAsync(string sellerId)
        {
            var orders = await _orders.Find(x => x.Seller.Id == sellerId).ToListAsync();
            return orders.Select(ToResponseDto).ToList();
        }

        public async Task<List<OrderResponseDto>> GetAllOrdersAsync()
        {
            var orders = await _orders.Find(_ => true).ToListAsync();
            return orders.Select(ToResponseDto).ToList();
        }

        public async Task<bool> UpdateOrderStatusAsync(string id, OrderStatus newStatus, string updatedById, string updatedBy)
        {
            var order = await _orders.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (order == null)
                throw new ArgumentException($"Order with ID {id} not found");

            var oldStatus = order.Status;
            order.UpdateStatus(newStatus, updatedById, updatedBy);

            var update = Builders<Order>.Update
                .Set(o => o.Status, order.Status)
                .Set(o => o.Timeline, order.Timeline)
                .Set(o => o.UpdatedAt, DateTime.UtcNow);

            var result = await _orders.UpdateOneAsync(x => x.Id == id, update);

            if (result.ModifiedCount > 0)
            {
                if (newStatus == OrderStatus.Delivered)
                    await UpdateProductStatusInternalAsync(order.Product.Id, "Sold");
                else if (newStatus == OrderStatus.Cancelled)
                    await UpdateProductStatusInternalAsync(order.Product.Id, "Published");
            }

            return result.ModifiedCount > 0;
        }

        public async Task<(List<OrderResponseDto>, int)> GetAllOrdersPagedAsync(int page, int pageSize)
        {
            var orders = await _orders.Find(_ => true)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            var totalCount = await _orders.CountDocumentsAsync(_ => true);
            return (orders.Select(ToResponseDto).ToList(), (int)totalCount);
        }

        public async Task<bool> CancelOrderAsync(string id, string userId, string userName)
        {
            var order = await _orders.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (order == null)
                throw new ArgumentException($"Order with ID {id} not found");

            // Chỉ cho phép buyer hoặc seller hủy trước khi admin chuyển sang Processing
            if (order.Status != OrderStatus.Pending)
                throw new InvalidOperationException("Chỉ được hủy đơn khi trạng thái là Pending (chưa xử lý)");

            if (userId != order.Buyer.Id && userId != order.Seller.Id)
                throw new UnauthorizedAccessException("Chỉ người mua hoặc người bán mới được hủy đơn hàng");

            order.Cancel(userId, userName);

            var update = Builders<Order>.Update
                .Set(o => o.Status, order.Status)
                .Set(o => o.Timeline, order.Timeline)
                .Set(o => o.UpdatedAt, DateTime.UtcNow);

            var result = await _orders.UpdateOneAsync(x => x.Id == id, update);

            if (result.ModifiedCount > 0)
                await UpdateProductStatusInternalAsync(order.Product.Id, "Published");

            return result.ModifiedCount > 0;
        }

        private static OrderResponseDto ToResponseDto(Order order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                Buyer = new UserBriefDto
                {
                    Id = order.Buyer.Id,
                    Name = order.Buyer.Name,
                    Email = order.Buyer.Email,
                    Phone = order.Buyer.Phone
                },
                Seller = new UserBriefDto
                {
                    Id = order.Seller.Id,
                    Name = order.Seller.Name,
                    Email = order.Seller.Email,
                    Phone = order.Seller.Phone
                },
                Product = new ProductDto
                {
                    Id = order.Product.Id,
                    Name = order.Product.Name,
                    Brand = order.Product.Brand,
                    Type = order.Product.Type,
                    Capacity = order.Product.Capacity,
                    Condition = order.Product.Condition,
                    Year = order.Product.Year,
                    Price = order.Product.Price,
                    Voltage = order.Product.Voltage,
                    CycleCount = order.Product.CycleCount,
                    Location = order.Product.Location,
                    Warranty = order.Product.Warranty,
                    Status = order.Product.Status,
                    Images = order.Product.Images,
                    Description = order.Product.Description,
                    OwnerId = order.Product.OwnerId
                },
                ShippingFee = order.ShippingFee,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                ShippingAddress = order.ShippingAddress,
                Notes = order.Notes,
                Status = order.Status.ToString(),
                Timeline = order.Timeline.Select(t => new OrderTimelineDto
                {
                    FromStatus = t.FromStatus,
                    ToStatus = t.ToStatus,
                    UpdatedById = t.UpdatedById,
                    UpdatedBy = t.UpdatedBy,
                    UpdatedAt = t.UpdatedAt
                }).ToList()
            };
        }
    }
}