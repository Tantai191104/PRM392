using System;
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
            public string UserId { get; set; } = string.Empty;
        }

        // ===================== CREATE ORDER =====================
        [HttpPost("create-order")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (dto.Amount <= 0 || string.IsNullOrWhiteSpace(dto.Description) || string.IsNullOrWhiteSpace(dto.UserId))
                return BadRequest(new { success = false, message = "Amount, Description and UserId are required" });

            string callbackUrl = "https://3c11f853370e.ngrok-free.app/api/zalopay/callback";

            var (success, error, appTransId, orderUrl) = await _zalopayService.CreateOrderAsync(dto.Amount, dto.Description, callbackUrl, dto.UserId);
            if (success)
            {
                // Cộng tiền và ghi transaction ngay khi tạo order
                var wallet = await _walletAppService.GetWalletByUserIdAsync(dto.UserId);
                if (wallet != null)
                {
                    wallet.Balance += dto.Amount;
                    await _walletAppService.UpdateWalletAsync(wallet);

                    var transaction = new Transaction
                    {
                        Id = Guid.NewGuid().ToString(),
                        WalletId = wallet.Id,
                        Amount = dto.Amount,
                        Type = "Deposit",
                        CreatedAt = DateTime.UtcNow,
                        Description = $"ZaloPay deposit: {appTransId}"
                    };
                    await _transactionService.CreateTransactionAsync(transaction);
                }
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        AppTransId = appTransId,
                        OrderUrl = orderUrl
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
            string key2 = "uUfsWgfLkRLzq6W2uNXTCxrfxs51auny"; // Thay bằng key2 của bạn

            try
            {
                string dataStr = body.GetProperty("data").GetString() ?? "";
                string reqMac = body.GetProperty("mac").GetString() ?? "";

                // Tính toán mac
                string mac = ZaloPayHelper.ComputeHmacSHA256(key2, dataStr);

                if (!reqMac.Equals(mac))
                {
                    // callback không hợp lệ
                    result["returncode"] = -1;
                    result["returnmessage"] = "mac not equal";
                }
                else
                {
                    // Parse data
                    var dataJson = JsonDocument.Parse(dataStr).RootElement;
                    string appTransId = dataJson.GetProperty("apptransid").GetString() ?? "";
                    long amount = dataJson.GetProperty("amount").GetInt64();
                    string userId = dataJson.TryGetProperty("userid", out var userIdProp) ? userIdProp.GetString() ?? "" : "";
                    string zpTransId = dataJson.TryGetProperty("zptransid", out var zpTransIdProp) ? zpTransIdProp.GetString() ?? "" : "";
                    long serverTime = dataJson.TryGetProperty("servertime", out var serverTimeProp) ? serverTimeProp.GetInt64() : 0;

                    Console.WriteLine($"[ZLP][CALLBACK] appTransId={appTransId} userId={userId} amount={amount} zpTransId={zpTransId}");

                    // Cộng tiền cho user
                    if (!string.IsNullOrEmpty(userId))
                    {
                        bool credited = await _zalopayService.CheckAndCreditOrderAsync(appTransId, userId);
                        if (credited)
                        {
                            var wallet = await _walletAppService.GetWalletByUserIdAsync(userId);
                            if (wallet != null)
                            {
                                var transaction = new Transaction
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    WalletId = wallet.Id,
                                    Amount = amount,
                                    Type = "Deposit",
                                    CreatedAt = DateTime.UtcNow,
                                    Description = $"ZaloPay deposit: {appTransId}"
                                };
                                await _transactionService.CreateTransactionAsync(transaction);
                                Console.WriteLine($"[Callback] Payment success: {appTransId}, amount: {amount}, user: {userId}");
                            }
                        }
                    }

                    result["returncode"] = 1;
                    result["returnmessage"] = "success";
                }
            }
            catch (Exception ex)
            {
                result["returncode"] = 0; // ZaloPay server sẽ callback lại (tối đa 3 lần)
                result["returnmessage"] = ex.Message;
            }

            return Ok(result);
        }
    }
}
