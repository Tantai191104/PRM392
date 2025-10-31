using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WalletService.Application.Services;

namespace WalletService.Web.Controllers
{
    [ApiController]
    [Route("api/zalopay")]
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
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (dto.Amount <= 0 || string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest("Amount and Description are required");

            string callbackUrl = "https://9d500abdec62.ngrok-free.app/api/zalopay/callback";

            // Gọi service tạo order
            var result = await _zalopayService.CreateOrderAsync(dto.Amount, dto.Description, callbackUrl);

            return Ok(result);
        }

        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromBody] JsonElement body)
        {
            string returnCode = body.GetProperty("returncode").GetString() ?? "";
            string appTransId = body.GetProperty("apptransid").GetString() ?? "";
            long amount = body.GetProperty("amount").GetInt64();
            string mac = body.GetProperty("mac").GetString() ?? "";
            string userId = "";
            if (body.TryGetProperty("userid", out var userIdProp))
                userId = userIdProp.GetString() ?? "";

            if (returnCode == "1" && !string.IsNullOrEmpty(userId))
            {
                // Add money to user's wallet
                await _walletAppService.ReleaseAsync(userId, amount);
                // Create transaction
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
            // ZaloPay docs yêu cầu trả về kiểu này
            return Ok(new { returncode = 1, returnmessage = "OK" });
        }
    }
}
