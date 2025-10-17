using MongoDB.Driver;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using SharedKernel.Entities;

namespace OrderService.Application.Services
{
    public interface IOrderAppService
    {
        Task<OrderResponseDto> CreateOrderAsync(OrderDto dto, string buyerId);
        Task<OrderResponseDto?> GetOrderByIdAsync(string id);
        Task<List<OrderResponseDto>> GetOrdersByBuyerAsync(string buyerId);
        Task<List<OrderResponseDto>> GetOrdersBySellerAsync(string sellerId);
        Task<List<OrderResponseDto>> GetAllOrdersAsync();
        Task<bool> UpdateOrderStatusAsync(string id, OrderService.Domain.Entities.OrderStatus status);
        Task<(List<OrderResponseDto>, int)> GetAllOrdersPagedAsync(int page, int pageSize);
        Task<bool> CancelOrderAsync(string id, string userId);
    }

    public class OrderAppService : IOrderAppService
    {
        private readonly IMongoCollection<Order> _orders;
        // Đã có khai báo và constructor đúng phía trên, xóa phần thừa này

        public async Task<OrderResponseDto> CreateOrderAsync(OrderDto dto, string buyerId)
        {
            // Fetch product details from ProductService
            var product = await GetProductAsync(dto.ProductId);

            var order = new Order
            {
                Buyer = new User
                {
                    Id = buyerId
                },
                Seller = new User
                {
                    Id = product.OwnerId,
                    Name = product.Brand, // Assuming Brand as Seller Name for now
                    Email = "seller@example.com" // Placeholder email
                },
                Product = product,
                TotalAmount = product.Price,
                Status = OrderStatus.Pending // Automatically set status to Pending
            };
            await _orders.InsertOneAsync(order);

            // Call ProductService to set product status to 'InTransaction'
            await HideProductAsync(dto.ProductId);

            return ToResponseDto(order);
        }

        // Hàm gọi ProductService để cập nhật trạng thái sản phẩm
        private readonly string _productServiceBaseUrl;

        public OrderAppService(IMongoDatabase database, IConfiguration config)
        {
            _orders = database.GetCollection<Order>("Orders");
            _productServiceBaseUrl = config["ExternalServices:ProductService:BaseUrl"] ?? "http://productservice:5137";
        }

        private async Task HideProductAsync(string productId)
        {
            using var client = new HttpClient();
            var url = $"{_productServiceBaseUrl}/api/products/{productId}/status";
            var content = new StringContent("{\"status\":\"InTransaction\"}", System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync(url, content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<Product> GetProductAsync(string productId)
        {
            using var client = new HttpClient();
            var url = $"{_productServiceBaseUrl}/api/products/{productId}";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to fetch product with ID {productId}");

            var json = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<Product>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Product deserialization failed");
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

        public async Task<bool> UpdateOrderStatusAsync(string id, OrderService.Domain.Entities.OrderStatus newStatus)
        {
            var order = await _orders.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (order == null)
                throw new ArgumentException($"Order with ID {id} not found");

            order.UpdateStatus(newStatus);

            var update = Builders<Order>.Update.Set(o => o.Status, order.Status);
            var result = await _orders.UpdateOneAsync(x => x.Id == id, update);

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

        public async Task<bool> CancelOrderAsync(string id, string userId)
        {
            var order = await _orders.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (order == null)
                throw new ArgumentException($"Order with ID {id} not found");

            if (order.Status != OrderStatus.Pending)
                throw new InvalidOperationException("Only pending orders can be canceled.");

            if (order.Buyer.Id != userId)
                throw new UnauthorizedAccessException("You are not authorized to cancel this order.");

            var update = Builders<Order>.Update.Set(o => o.Status, OrderStatus.Cancelled);
            var result = await _orders.UpdateOneAsync(x => x.Id == id, update);

            return result.ModifiedCount > 0;
        }

        private OrderResponseDto ToResponseDto(Order order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                Buyer = new UserDto
                {
                    Id = order.Buyer.Id,
                    Name = order.Buyer.Name,
                    Email = order.Buyer.Email
                },
                Seller = new UserDto
                {
                    Id = order.Seller.Id,
                    Name = order.Seller.Name,
                    Email = order.Seller.Email
                },
                ProductId = order.Product.Id,
                ProductName = order.Product.Name,
                ProductPrice = order.Product.Price,
                TotalAmount = order.TotalAmount,
                Status = order.Status
            };
        }
    }
}