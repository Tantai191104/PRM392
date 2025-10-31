#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletService.Application.Services;
using WalletService.Domain.Entities;

namespace WalletService.Web.Controllers
{
    [ApiController]
    [Route("api/zalopay")]
    public class ZaloPayController : ControllerBase
    {
        private readonly ZaloPayService _zalopayService;
        private readonly WalletAppService _walletAppService;
        private readonly TransactionService _transactionService;
        private const string CallbackKey = "uUfsWgfLkRLzq6W2uNXTCxrfxs51auny";

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

        [HttpPost("create-order")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (dto.Amount <= 0 || string.IsNullOrWhiteSpace(dto.Description) || string.IsNullOrWhiteSpace(dto.UserId))
                return BadRequest(new { success = false, message = "Invalid input." });

            const string callbackUrl = "https://3c11f853370e.ngrok-free.app/api/zalopay/callback";

            var (success, error, appTransId, orderUrl) = await _zalopayService.CreateOrderAsync(
                dto.Amount, dto.Description, callbackUrl, dto.UserId);

            return success
                ? Ok(new { success = true, data = new { AppTransId = appTransId, OrderUrl = orderUrl } })
                : BadRequest(new { success = false, message = error ?? "Create order failed." });
        }

        [HttpPost("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> Callback([FromBody] JsonElement body)
        {
            var result = new Dictionary<string, object>();

            try
            {
                string dataStr = body.GetProperty("data").GetString() ?? string.Empty;
                string reqMac = body.GetProperty("mac").GetString() ?? string.Empty;

                Console.WriteLine($"[ZLP][RAW DATA] {dataStr}"); // ← IN RA ĐỂ XEM

                string mac = ZaloPayHelper.ComputeHmacSHA256(dataStr, CallbackKey);
                if (!reqMac.Equals(mac, StringComparison.OrdinalIgnoreCase))
                {
                    result["return_code"] = -1;
                    result["return_message"] = "mac not equal";
                    return Ok(result);
                }

                var dataJson = JsonDocument.Parse(dataStr).RootElement;

                // IN RA TẤT CẢ CÁC FIELD
                foreach (var prop in dataJson.EnumerateObject())
                {
                    Console.WriteLine($"[ZLP][FIELD] {prop.Name} = {prop.Value}");
                }

                string appTransId = dataJson.GetProperty("apptransid").GetString() ?? string.Empty;
                int status = dataJson.GetProperty("status").GetInt32();
                long amount = dataJson.GetProperty("amount").GetInt64();

                // THỬ LẤY userid HOẶC appuser
                string? userId = dataJson.TryGetProperty("userid", out var uid) ? uid.GetString() :
                                dataJson.TryGetProperty("appuser", out var au) ? au.GetString() : null;

                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("[ZLP][ERROR] userId/appuser not found in callback!");
                    result["return_code"] = 0;
                    result["return_message"] = "missing userId";
                    return Ok(result);
                }

                Console.WriteLine($"[ZLP][CALLBACK] appTransId={appTransId} | status={status} | amount={amount} | userId={userId}");

                if (status == 1 && amount > 0)
                {
                    bool alreadyProcessed = await _transactionService.HasTransactionByReferenceIdAsync(appTransId);
                    if (alreadyProcessed)
                    {
                        Console.WriteLine($"[ZLP] Duplicate ignored: {appTransId}");
                    }
                    else
                    {
                        var wallet = await _walletAppService.GetWalletByUserIdAsync(userId);
                        if (wallet == null)
                        {
                            Console.WriteLine($"[ZLP][ERROR] Wallet not found for userId: {userId}");
                            result["return_code"] = 0;
                            result["return_message"] = "wallet not found";
                            return Ok(result);
                        }

                        wallet.Balance += amount;
                        await _walletAppService.UpdateWalletAsync(wallet);

                        var transaction = new Transaction
                        {
                            Id = Guid.NewGuid().ToString(),
                            WalletId = wallet.Id,
                            Amount = amount,
                            Type = "Deposit",
                            Description = $"ZaloPay deposit: {appTransId}",
                            CreatedAt = DateTime.UtcNow,
                            ReferenceId = appTransId
                        };

                        await _transactionService.CreateTransactionAsync(transaction);
                        Console.WriteLine($"[ZLP] SUCCESS: +{amount} vào ví {userId}");
                    }
                }

                result["return_code"] = 1;
                result["return_message"] = "success";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZLP][EXCEPTION] {ex.Message}\n{ex.StackTrace}");
                result["return_code"] = 0;
                result["return_message"] = "processing error";
            }

            return Ok(result);
        }
    }
}