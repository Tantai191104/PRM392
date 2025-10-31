using System;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WalletService.Application.Services;

namespace WalletService.Web.Controllers
{
    [ApiController]
    [Route("api/zalopay")]
    [Authorize] // Bật authorize cho tất cả route trong controller
    public class ZaloPayController : ControllerBase
    {
        private readonly ZaloPayService _zalopayService;
        private readonly WalletAppService _walletAppService;
        private readonly TransactionService _transactionService;

        public ZaloPayController(ZaloPayService zalopayService, WalletAppService walletAppService, TransactionService transactionService)
        {
            _zalopayService = zalopayService;
            _walletAppService = walletAppService;
            _transactionService = transactionService;
        }

        public class CreateOrderDto
        {
            public long Amount { get; set; }
            public string Description { get; set; } = string.Empty;
        }

        [HttpPost("create-order")]
        [AllowAnonymous] // Nếu muốn public route này
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (dto.Amount <= 0 || string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest("Amount and Description are required");

            string callbackUrl = "https://your-callback-url/api/zalopay/callback";

            var result = await _zalopayService.CreateOrderAsync(dto.Amount, dto.Description, callbackUrl);

            return Ok(result);
        }

        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromBody] JsonElement body)
        {
            string returnCode = body.GetProperty("returncode").GetString() ?? "";
            string appTransId = body.GetProperty("apptransid").GetString() ?? "";
            long amount = body.GetProperty("amount").GetInt64();
            string userId = "";

            if (body.TryGetProperty("userid", out var userIdProp))
                userId = userIdProp.GetString() ?? "";

            // Nếu không có userid trong body, lấy từ JWT token
            if (string.IsNullOrEmpty(userId))
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
                if (claim != null)
                    userId = claim.Value;
            }

            if (returnCode == "1" && !string.IsNullOrEmpty(userId))
            {
                await _walletAppService.ReleaseAsync(userId, amount);

                var transaction = new WalletService.Domain.Entities.Transaction
                {
                    Id = Guid.NewGuid().ToString(),
                    WalletId = (await _walletAppService.GetWalletByUserIdAsync(userId))?.Id ?? "",
                    Amount = amount,
                    Type = "Deposit",
                    CreatedAt = DateTime.UtcNow,
                    Description = $"ZaloPay deposit: {appTransId}"
                };
                await _transactionService.CreateTransactionAsync(transaction);

                Console.WriteLine($"Payment success: {appTransId}, amount: {amount}, user: {userId}");
            }
            else
            {
                Console.WriteLine($"Payment failed: {appTransId}");
            }

            return Ok(new { returncode = 1, returnmessage = "OK" });
        }
    }
}
