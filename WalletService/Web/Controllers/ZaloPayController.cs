using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletService.Application.Services;
using WalletService.Domain.Entities;

#nullable enable

namespace WalletService.Web.Controllers
{
    [ApiController]
    [Route("api/zalopay")]
    public class ZaloPayController : ControllerBase
    {
        private readonly ZaloPayService _zalopayService;
        private readonly WalletAppService _walletAppService;
        private readonly TransactionService _transactionService;

        public ZaloPayController(
            ZaloPayService zalopayService,
            WalletAppService walletAppService,
            TransactionService transactionService)
        {
            _zalopayService = zalopayService;
            _walletAppService = walletAppService;
            _transactionService = transactionService;
        }

        public class CreateOrderDto
        {
            public long Amount { get; set; }
            public string Description { get; set; } = string.Empty;
            public string? RedirectUrl { get; set; }  // URL để redirect về sau khi thanh toán
        }

        // ===================== CREATE ORDER =====================
        [HttpPost("create-order")]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            // Lấy userId từ JWT token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });
            
            string userId = userIdClaim.Value;

            if (dto.Amount <= 0 || string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest(new { success = false, message = "Amount and Description are required" });

            // Validate wallet exists
            var wallet = await _walletAppService.GetWalletByUserIdAsync(userId);
            if (wallet == null)
            {
                return BadRequest(new { success = false, message = "Wallet not found for user" });
            }

            string callbackUrl = "https://3c11f853370e.ngrok-free.app/api/zalopay/callback";
            
            // Nếu không có redirectUrl, dùng default
            string redirectUrl = string.IsNullOrWhiteSpace(dto.RedirectUrl) 
                ? "http://localhost:3000/payment-success"  // Thay bằng URL frontend của bạn
                : dto.RedirectUrl;

            var (success, error, appTransId, orderUrl) = await _zalopayService.CreateOrderAsync(
                dto.Amount, 
                dto.Description, 
                callbackUrl, 
                userId,
                redirectUrl
            );

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        AppTransId = appTransId,
                        OrderUrl = orderUrl,
                        RedirectUrl = redirectUrl
                    }
                });
            }
            else
            {
                return BadRequest(new { success = false, message = error ?? "Unknown error" });
            }
        }

        // ===================== CALLBACK =====================
        [HttpPost("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> Callback([FromBody] JsonElement body)
        {
            var result = new Dictionary<string, object>();
            string key2 = "uUfsWgfLkRLzq6W2uNXTCxrfxs51auny";

            try
            {
                string dataStr = body.GetProperty("data").GetString() ?? "";
                string reqMac = body.GetProperty("mac").GetString() ?? "";

                // Verify mac
                string mac = ZaloPayHelper.ComputeHmacSHA256(key2, dataStr);

                Console.WriteLine($"[ZLP][CALLBACK] Received data: {dataStr}");
                Console.WriteLine($"[ZLP][CALLBACK] Received mac: {reqMac}");
                Console.WriteLine($"[ZLP][CALLBACK] Computed mac: {mac}");

                if (!reqMac.Equals(mac))
                {
                    Console.WriteLine("[ZLP][CALLBACK] MAC verification failed!");
                    result["returncode"] = -1;
                    result["returnmessage"] = "mac not equal";
                    return Ok(result);
                }

                // Parse data
                var dataJson = JsonDocument.Parse(dataStr).RootElement;
                string appTransId = dataJson.GetProperty("app_trans_id").GetString() ?? "";
                long amount = dataJson.GetProperty("amount").GetInt64();
                
                // Get userId from app_user field
                string userId = dataJson.TryGetProperty("app_user", out var appUserProp) 
                    ? appUserProp.GetString() ?? "" 
                    : "";

                string zpTransId = dataJson.TryGetProperty("zp_trans_id", out var zpTransIdProp) 
                    ? zpTransIdProp.GetString() ?? "" 
                    : "";

                Console.WriteLine($"[ZLP][CALLBACK] appTransId={appTransId}, userId={userId}, amount={amount}, zpTransId={zpTransId}");

                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("[ZLP][CALLBACK] UserId is empty!");
                    result["returncode"] = -1;
                    result["returnmessage"] = "userId is empty";
                    return Ok(result);
                }

                // Get wallet
                var wallet = await _walletAppService.GetWalletByUserIdAsync(userId);
                if (wallet == null)
                {
                    Console.WriteLine($"[ZLP][CALLBACK] Wallet not found for userId: {userId}");
                    result["returncode"] = -1;
                    result["returnmessage"] = "wallet not found";
                    return Ok(result);
                }

                // Check if transaction already exists (prevent duplicate)
                var existingTransactions = await _transactionService.GetTransactionsByWalletIdAsync(wallet.Id);
                bool alreadyProcessed = existingTransactions.Any(t => t.Description.Contains(appTransId));

                if (alreadyProcessed)
                {
                    Console.WriteLine($"[ZLP][CALLBACK] Transaction already processed: {appTransId}");
                    result["returncode"] = 1;
                    result["returnmessage"] = "success (already processed)";
                    return Ok(result);
                }

                // Credit wallet
                wallet.Balance += amount;
                await _walletAppService.UpdateWalletAsync(wallet);

                // Create transaction record
                var transaction = new Transaction
                {
                    Id = Guid.NewGuid().ToString(),
                    WalletId = wallet.Id,
                    Amount = amount,
                    Type = "Deposit",
                    CreatedAt = DateTime.UtcNow,
                    Description = $"ZaloPay deposit: {appTransId} (zpTransId: {zpTransId})"
                };
                await _transactionService.CreateTransactionAsync(transaction);

                Console.WriteLine($"[ZLP][CALLBACK] Payment success: {appTransId}, amount: {amount}, user: {userId}, new balance: {wallet.Balance}");

                result["returncode"] = 1;
                result["returnmessage"] = "success";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZLP][CALLBACK] Error: {ex.Message}");
                Console.WriteLine($"[ZLP][CALLBACK] StackTrace: {ex.StackTrace}");
                result["returncode"] = 0; // ZaloPay will retry
                result["returnmessage"] = ex.Message;
            }

            return Ok(result);
        }

        // ===================== CHECK PAYMENT STATUS =====================
        // Frontend gọi endpoint này sau khi user thanh toán để check trạng thái
        [HttpPost("check-payment")]
        [Authorize]
        public async Task<IActionResult> CheckPayment([FromBody] CheckPaymentDto dto)
        {
            // Lấy userId từ JWT token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });
            
            string userId = userIdClaim.Value;

            if (string.IsNullOrWhiteSpace(dto.AppTransId))
                return BadRequest(new { success = false, message = "AppTransId is required" });

            // Check status từ ZaloPay
            var status = await _zalopayService.QueryOrderStatusAsync(dto.AppTransId);
            
            // Nếu query thất bại
            if (!status.TryGetValue("returncode", out var rcObj) || Convert.ToInt32(rcObj) != 1)
            {
                return Ok(new
                {
                    success = false,
                    paid = false,
                    message = "Order not found or payment pending"
                });
            }

            // Check xem đã thanh toán chưa (có zptransid = đã thanh toán)
            bool isPaid = status.TryGetValue("zptransid", out var zpTransIdObj) && 
                          zpTransIdObj != null && 
                          !string.IsNullOrEmpty(zpTransIdObj.ToString());
            
            if (!isPaid)
            {
                return Ok(new
                {
                    success = true,
                    paid = false,
                    message = "Payment not completed yet. Please complete payment on ZaloPay."
                });
            }

            // Đã thanh toán → Check xem đã cộng tiền chưa
            var wallet = await _walletAppService.GetWalletByUserIdAsync(userId);
            if (wallet == null)
            {
                return BadRequest(new { success = false, message = "Wallet not found" });
            }

            var transactions = await _transactionService.GetTransactionsByWalletIdAsync(wallet.Id);
            bool alreadyCredited = transactions.Any(t => t.Description.Contains(dto.AppTransId));

            if (alreadyCredited)
            {
                // Đã cộng tiền rồi
                return Ok(new
                {
                    success = true,
                    paid = true,
                    credited = true,
                    message = "Payment already processed",
                    balance = wallet.Balance
                });
            }

            // Chưa cộng tiền → Cộng tiền ngay
            var (success, message, amount) = await _zalopayService.CheckAndCreditOrderAsync(dto.AppTransId, userId);

            if (success)
            {
                wallet = await _walletAppService.GetWalletByUserIdAsync(userId);
                return Ok(new
                {
                    success = true,
                    paid = true,
                    credited = true,
                    message = "Payment successful! Wallet credited.",
                    amount = amount,
                    balance = wallet?.Balance ?? 0
                });
            }
            else
            {
                return Ok(new
                {
                    success = false,
                    paid = true,
                    credited = false,
                    message = message
                });
            }
        }

        public class CheckPaymentDto
        {
            public string AppTransId { get; set; } = string.Empty;
        }
    }
}
