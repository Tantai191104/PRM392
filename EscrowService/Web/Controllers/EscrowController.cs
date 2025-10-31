using EscrowService.Application.DTOs;
using EscrowService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EscrowService.Web.Controllers
{
    [ApiController]
    [Route("api/escrows")]
    public class EscrowController : ControllerBase
    {
        private readonly IEscrowAppService _escrowService;
        private readonly ILogger<EscrowController> _logger;

        public EscrowController(IEscrowAppService escrowService, ILogger<EscrowController> logger)
        {
            _escrowService = escrowService;
            _logger = logger;
        }
        /// <summary>
        /// Get all escrows with filter (admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin , Staff")]
        public async Task<IActionResult> GetAllEscrows([FromQuery] string? status, [FromQuery] string? buyerId, [FromQuery] string? sellerId, [FromQuery] string? orderId)
        {
            try
            {
                var filters = new EscrowFilterDto
                {
                    Status = status,
                    BuyerId = buyerId,
                    SellerId = sellerId,
                    OrderId = orderId
                };

                var escrows = await _escrowService.GetAllEscrowsAsync(filters);
                return Ok(new { success = true, data = escrows, count = escrows.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all escrows (admin)");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
        /// <summary>
        /// Create escrow (initiates Saga)
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateEscrow([FromBody] CreateEscrowDto dto)
        {
            try
            {
                var buyerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? Request.Headers["X-User-Id"].FirstOrDefault();

                if (string.IsNullOrEmpty(buyerId))
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                // Truyền OrderId từ query nếu có
                var orderId = Request.Query["orderId"].FirstOrDefault();
                if (!string.IsNullOrEmpty(orderId))
                    dto.OrderId = orderId;

                var escrow = await _escrowService.CreateEscrowAsync(dto, buyerId);
                return Ok(new { success = true, data = escrow });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating escrow");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get escrow by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var escrow = await _escrowService.GetEscrowByIdAsync(id);
                if (escrow == null)
                    return NotFound(new { success = false, message = "Escrow not found" });

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("sub")?.Value;
                var isAdmin = User.IsInRole("Admin");

                // Only buyer, seller, or admin can view
                if (!isAdmin && userId != escrow.BuyerId && userId != escrow.SellerId)
                    return Forbid();

                return Ok(new { success = true, data = escrow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escrow {EscrowId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get escrow by order ID
        /// </summary>
        [HttpGet("order/{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetByOrderId(string orderId)
        {
            try
            {
                var escrow = await _escrowService.GetEscrowByOrderIdAsync(orderId);
                if (escrow == null)
                    return NotFound(new { success = false, message = "Escrow not found for this order" });

                return Ok(new { success = true, data = escrow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting escrow for order {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get buyer's escrows
        /// </summary>
        [HttpGet("buyer")]
        [Authorize]
        public async Task<IActionResult> GetBuyerEscrows()
        {
            try
            {
                var buyerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(buyerId))
                    return Unauthorized();

                var escrows = await _escrowService.GetEscrowsByBuyerAsync(buyerId);
                return Ok(new { success = true, data = escrows, count = escrows.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting buyer escrows");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get seller's escrows
        /// </summary>
        [HttpGet("seller")]
        [Authorize]
        public async Task<IActionResult> GetSellerEscrows()
        {
            try
            {
                var sellerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(sellerId))
                    return Unauthorized();

                var escrows = await _escrowService.GetEscrowsBySellerAsync(sellerId);
                return Ok(new { success = true, data = escrows, count = escrows.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller escrows");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Release escrow to seller
        /// </summary>
        [HttpPost("{id}/release")]
        [Authorize] // Chỉ cần JWT Bearer
        public async Task<IActionResult> ReleaseEscrow(string id, [FromBody] ReleaseEscrowDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var escrow = await _escrowService.GetEscrowByIdAsync(id);
            if (escrow == null)
                return NotFound(new { success = false, message = "Escrow not found" });

            // Chỉ seller hoặc admin/staff mới release
            var isAdminOrStaff = User.IsInRole("Admin") || User.IsInRole("Staff");
            if (!isAdminOrStaff && userId != escrow.SellerId)
                return Forbid();

            var resultBefore = await _escrowService.GetEscrowByIdAsync(id);
            var resultAfter = await _escrowService.ReleaseEscrowAsync(id, userId, dto, isAdminOrStaff);

            // Nếu trạng thái escrow không đổi (chuyển tiền thất bại), trả về success=false
            if (resultBefore != null && resultAfter != null && resultBefore.Status == resultAfter.Status)
            {
                return Ok(new { success = false, message = "Không thể chuyển tiền cho người bán. Vui lòng thử lại.", data = resultAfter });
            }
            // Nếu chuyển tiền thành công (trạng thái thay đổi), trả về success=true
            return Ok(new { success = true, message = "Escrow released to seller", data = resultAfter });
        }

        /// <summary>
        /// Refund escrow to buyer
        /// </summary>
        [HttpPost("{id}/refund")]
        [Authorize] // Chỉ cần JWT Bearer
        public async Task<IActionResult> RefundEscrow(string id, [FromBody] RefundEscrowDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var escrow = await _escrowService.GetEscrowByIdAsync(id);
            if (escrow == null)
                return NotFound(new { success = false, message = "Escrow not found" });

            // Chỉ buyer hoặc admin/staff mới refund
            var isAdminOrStaff = User.IsInRole("Admin") || User.IsInRole("Staff");
            if (!isAdminOrStaff && userId != escrow.BuyerId)
                return Forbid();

            var resultBefore = await _escrowService.GetEscrowByIdAsync(id);
            var resultAfter = await _escrowService.RefundEscrowAsync(id, userId, dto, isAdminOrStaff);

            // Nếu trạng thái escrow không đổi (hoàn tiền thất bại), trả về success=false
            if (resultBefore != null && resultAfter != null && resultBefore.Status == resultAfter.Status)
            {
                return Ok(new { success = false, message = "Không thể hoàn tiền cho người mua. Vui lòng thử lại.", data = resultAfter });
            }
            // Nếu hoàn tiền thành công (trạng thái thay đổi), trả về success=true
            return Ok(new { success = true, message = "Escrow refunded to buyer", data = resultAfter });
        }
    }
}

