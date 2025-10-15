using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;

namespace OrderService.Application.Services
{
    public interface IOrderAppService
    {
        Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto);
        Task<IEnumerable<OrderResponseDto>> GetOrdersAsync();
        Task<OrderResponseDto?> GetOrderByIdAsync(string id);
        Task<IEnumerable<OrderResponseDto>> GetOrdersByUserIdAsync(int userId);
    }

    public class OrderAppService : IOrderAppService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IAuthService _authService;
        private readonly IProductService _productService;
        private readonly ILogger<OrderAppService> _logger;

        public OrderAppService(
            IOrderRepository orderRepository,
            IAuthService authService,
            IProductService productService,
            ILogger<OrderAppService> logger)
        {
            _orderRepository = orderRepository;
            _authService = authService;
            _productService = productService;
            _logger = logger;
        }

        public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto)
        {
            _logger.LogInformation("Creating order for UserId: {UserId}, ProductId: {ProductId}, Quantity: {Quantity}", 
                dto.UserId, dto.ProductId, dto.Quantity);

            // Get user information
            var user = await _authService.GetUserAsync(dto.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", dto.UserId);
                throw new ArgumentException($"User with ID {dto.UserId} not found");
            }

            // Get product information
            var product = await _productService.GetProductAsync(dto.ProductId);
            if (product == null)
            {
                _logger.LogWarning("Product not found: {ProductId}", dto.ProductId);
                throw new ArgumentException($"Product with ID {dto.ProductId} not found");
            }

            // Check stock availability
            if (product.Stock < dto.Quantity)
            {
                _logger.LogWarning("Insufficient stock. ProductId: {ProductId}, Available: {Stock}, Requested: {Quantity}", 
                    dto.ProductId, product.Stock, dto.Quantity);
                throw new InvalidOperationException($"Insufficient stock. Available: {product.Stock}, Requested: {dto.Quantity}");
            }

            // Create order
            var order = new Order
            {
                UserId = dto.UserId,
                UserEmail = user.Email,
                ProductId = dto.ProductId,
                ProductName = product.Name,
                ProductPrice = product.Price,
                Quantity = dto.Quantity,
                TotalAmount = product.Price * dto.Quantity,
                Status = OrderStatus.Pending
            };

            var createdOrder = await _orderRepository.CreateAsync(order);
            _logger.LogInformation("Order created successfully with ID: {OrderId}", createdOrder.Id);

            return MapToResponseDto(createdOrder);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetOrdersAsync()
        {
            _logger.LogInformation("Getting all orders");
            var orders = await _orderRepository.GetAllAsync();
            return orders.Select(MapToResponseDto);
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(string id)
        {
            _logger.LogInformation("Getting order by ID: {OrderId}", id);
            var order = await _orderRepository.GetByIdAsync(id);
            return order != null ? MapToResponseDto(order) : null;
        }

        public async Task<IEnumerable<OrderResponseDto>> GetOrdersByUserIdAsync(int userId)
        {
            _logger.LogInformation("Getting orders for UserId: {UserId}", userId);
            var orders = await _orderRepository.GetByUserIdAsync(userId);
            return orders.Select(MapToResponseDto);
        }

        private static OrderResponseDto MapToResponseDto(Order order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserEmail = order.UserEmail,
                ProductId = order.ProductId,
                ProductName = order.ProductName,
                ProductPrice = order.ProductPrice,
                Quantity = order.Quantity,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt ?? order.CreatedAt
            };
        }
    }
}