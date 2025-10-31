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

        public OrderController(IOrderAppService orderAppService)
        {
            _orderAppService = orderAppService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "Invalid or missing user token" });

            if (string.IsNullOrWhiteSpace(dto.ProductId))
                return BadRequest(new { success = false, message = "ProductId is required" });

            try
            {
                var product = await _orderAppService.GetProductForOrderAsync(dto.ProductId);
                if (product == null)
                    return BadRequest(new { success = false, message = "Product not found" });

                if (product.Status != "Published")
                    return BadRequest(new { success = false, message = "Product is not available for ordering" });

                // Kiểm tra người dùng không được mua sản phẩm của chính mình
                if (product.OwnerId == userId)
                    return BadRequest(new { success = false, message = "You cannot order your own product" });

                // Lấy JWT từ header Authorization
                var jwtToken = Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(jwtToken) && jwtToken.StartsWith("Bearer "))
                {
                    jwtToken = jwtToken.Substring(7);
                }

                var result = await _orderAppService.CreateOrderAsync(dto, userId, jwtToken);
                return Ok(new { success = true, message = "Order created successfully", data = result });
            }
            catch (MongoDB.Driver.MongoWriteException ex)
            {
                Console.WriteLine($"MongoDB Write Error: {ex.Message}");
                Console.WriteLine($"WriteError: {ex.WriteError}");
                return StatusCode(500, new { success = false, message = ex.Message, details = ex.WriteError?.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating order: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var (orders, totalCount) = await _orderAppService.GetAllOrdersPagedAsync(page, pageSize);
            var response = new
            {
                total = totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                items = orders
            };
            return Ok(response);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrderById(string id)
        {
            var order = await _orderAppService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound(new { success = false, message = $"Order with ID {id} not found" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && userId != order.Buyer.Id && userId != order.Seller.Id)
                return Forbid();

            return Ok(order);
        }

        [HttpGet("buyer")]
        [Authorize]
        public async Task<IActionResult> GetMyOrdersAsBuyer()
        {
            var buyerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(buyerId))
                return Unauthorized();

            var orders = await _orderAppService.GetOrdersByBuyerAsync(buyerId);
            return Ok(orders);
        }

        [HttpGet("seller")]
        [Authorize]
        public async Task<IActionResult> GetMyOrdersAsSeller()
        {
            var sellerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(sellerId))
                return Unauthorized();

            var orders = await _orderAppService.GetOrdersBySellerAsync(sellerId);
            return Ok(orders);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateOrderStatus(string id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new { success = false, message = "Status is required" });

            var order = await _orderAppService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound(new { success = false, message = $"Order with ID {id} not found" });

            if (!Enum.TryParse(dto.Status, out OrderStatus parsedStatus))
                return BadRequest(new { success = false, message = "Invalid status value" });

            if (!Enum.TryParse<OrderStatus>(order.Status, out var currentStatus))
                return BadRequest(new { success = false, message = "Invalid current order status" });

            if (!IsValidStatusTransition(currentStatus, parsedStatus))
                return BadRequest(new { success = false, message = $"Cannot change status from {order.Status} to {parsedStatus}" });

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(new { success = false, message = "Missing or invalid user ID" });

                var updated = await _orderAppService.UpdateOrderStatusAsync(id, parsedStatus, userId, userName);

                return updated
                    ? Ok(new { success = true, message = "Order status updated successfully" })
                    : BadRequest(new { success = false, message = "Failed to update order status" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            try
            {
                var cancelled = await _orderAppService.CancelOrderAsync(id, userId, userName);

                if (cancelled)
                    return Ok(new { success = true, message = "Order cancelled successfully" });
                else
                    return BadRequest(new { success = false, message = "Failed to cancel order" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        [HttpGet("status/{orderId}")]
        public async Task<IActionResult> GetOrderStatus(string orderId)
        {
            var order = await _orderAppService.GetOrderByIdAsync(orderId);
            if (order == null)
                return NotFound(new { success = false, message = $"Order with ID {orderId} not found" });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && userId != order.Buyer.Id && userId != order.Seller.Id)
                return Forbid();

            return Ok(new { orderId = order.Id, status = order.Status });
        }
        private bool IsValidStatusTransition(OrderStatus current, OrderStatus next)
        {
            return current switch
            {
                OrderStatus.Pending => next is OrderStatus.Confirmed or OrderStatus.Cancelled,
                OrderStatus.Confirmed => next is OrderStatus.Shipped or OrderStatus.Cancelled,
                OrderStatus.Shipped => next is OrderStatus.Delivered or OrderStatus.Cancelled,
                OrderStatus.Delivered => false, // Không thể thay đổi sau khi delivered
                OrderStatus.Cancelled => false, // Không thể thay đổi sau khi cancelled
                _ => false
            };
        }
    }
}