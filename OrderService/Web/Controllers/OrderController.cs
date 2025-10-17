using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrderService.Application.DTOs;
using OrderService.Application.Services;
using System.Security.Claims;
using OrderService.Domain.Entities;

namespace OrderService.Web.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderAppService _orderAppService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderAppService orderAppService, ILogger<OrderController> logger)
        {
            _orderAppService = orderAppService;
            _logger = logger;
        }

        // =============================
        // POST: /api/orders
        // =============================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Automatically detect BuyerId from token
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized("Invalid or missing user token");

                // Ensure ProductId is provided
                if (string.IsNullOrWhiteSpace(dto.ProductId))
                    return BadRequest("ProductId is required");

                var result = await _orderAppService.CreateOrderAsync(dto, userId);
                _logger.LogInformation("Order created successfully: {OrderId}", result.Id);

                return CreatedAtAction(nameof(GetOrderById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when creating order");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, new { success = false, message = "An error occurred while creating the order" });
            }
        }

        // =============================
        // GET: /api/orders
        // Admin có thể xem tất cả, có pagination
        // =============================
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var (orders, totalCount) = await _orderAppService.GetAllOrdersPagedAsync(page, pageSize);

                var response = new
                {
                    total = totalCount,
                    page,
                    pageSize,
                    items = orders
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders");
                return StatusCode(500, "An error occurred while retrieving orders");
            }
        }

        // =============================
        // GET: /api/orders/{id}
        // =============================
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrderById(string id)
        {
            try
            {
                var order = await _orderAppService.GetOrderByIdAsync(id);
                if (order == null)
                    return NotFound(new { success = false, message = $"Order with ID {id} not found" });

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                // Chỉ buyer, seller hoặc admin mới được xem
                if (!isAdmin && userId != order.Buyer.Id && userId != order.Seller.Id)
                    return Forbid("You are not authorized to view this order");

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order with ID: {OrderId}", id);
                return StatusCode(500, "An error occurred while retrieving the order");
            }
        }

        // =============================
        // GET: /api/orders/buyer
        // Lấy danh sách đơn hàng của chính người mua đang đăng nhập
        // =============================
        [HttpGet("buyer")]
        [Authorize]
        public async Task<IActionResult> GetMyOrdersAsBuyer()
        {
            try
            {
                var buyerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(buyerId))
                    return Unauthorized();

                var orders = await _orderAppService.GetOrdersByBuyerAsync(buyerId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving buyer's orders");
                return StatusCode(500, "An error occurred while retrieving orders");
            }
        }

        // =============================
        // GET: /api/orders/seller
        // Lấy danh sách đơn hàng của người bán đang đăng nhập
        // =============================
        [HttpGet("seller")]
        [Authorize]
        public async Task<IActionResult> GetMyOrdersAsSeller()
        {
            try
            {
                var sellerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(sellerId))
                    return Unauthorized();

                var orders = await _orderAppService.GetOrdersBySellerAsync(sellerId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving seller's orders");
                return StatusCode(500, "An error occurred while retrieving orders");
            }
        }

        // =============================
        // PUT: /api/orders/{id}
        // Cập nhật trạng thái đơn hàng (ví dụ: Confirmed, Shipped, Completed)
        // =============================
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateOrderStatus(string id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest("Status is required");

            try
            {
                var order = await _orderAppService.GetOrderByIdAsync(id);
                if (order == null)
                    return NotFound(new { message = $"Order with ID {id} not found" });

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                // Chỉ seller hoặc admin được cập nhật
                if (!isAdmin && userId != order.Seller.Id)
                    return Forbid("You are not authorized to update this order");

                if (!Enum.TryParse(dto.Status, out OrderStatus parsedStatus))
                    return BadRequest("Invalid status value");

                var updated = await _orderAppService.UpdateOrderStatusAsync(id, parsedStatus);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return StatusCode(500, "An error occurred while updating the order status");
            }
        }
    }
}
