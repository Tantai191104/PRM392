using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WalletService.Application.DTOs;
using WalletService.Application.Services;
using WalletService.Domain.Entities;
using WalletService.Infrastructure.VNPay;

namespace WalletService.Web.Controllers
{
    [ApiController]
    [Route("api/vnpay")]
    public class VNPayController : ControllerBase
    {
        private readonly VNPayService _vnpayService;
        private readonly WalletAppService _walletService;
        private readonly TransactionService _transactionService;

        public VNPayController(
            VNPayService vnpayService, 
            WalletAppService walletService,
            TransactionService transactionService)
        {
            _vnpayService = vnpayService;
            _walletService = walletService;
            _transactionService = transactionService;
        }

        /// <summary>
        /// Create VNPay payment URL
        /// </summary>
        [HttpPost("create-payment-url")]
        [ProducesResponseType(typeof(VNPayResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult CreatePaymentUrl([FromBody] VNPayRequestDTO request)
        {
            if (request == null || request.Amount <= 0)
            {
                return BadRequest(new { message = "Invalid payment request" });
            }

            var response = _vnpayService.CreatePaymentUrl(request);
            return Ok(response);
        }

        /// <summary>
        /// VNPay callback/return URL endpoint
        /// </summary>
        [HttpGet("callback")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VNPayCallback()
        {
            var callbackData = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());

            // Validate signature
            if (!_vnpayService.ValidateCallback(callbackData))
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Invalid signature" 
                });
            }

            // Parse callback data
            var responseCode = callbackData.GetValueOrDefault("vnp_ResponseCode", "");
            var txnRef = callbackData.GetValueOrDefault("vnp_TxnRef", "");
            var amount = callbackData.GetValueOrDefault("vnp_Amount", "0");
            var bankCode = callbackData.GetValueOrDefault("vnp_BankCode", "");
            var transactionNo = callbackData.GetValueOrDefault("vnp_TransactionNo", "");
            var orderInfo = callbackData.GetValueOrDefault("vnp_OrderInfo", "");

            // Check if payment was successful
            if (responseCode != "00")
            {
                return Ok(new 
                { 
                    success = false, 
                    message = "Payment failed",
                    responseCode,
                    txnRef
                });
            }

            // Parse amount (VNPay sends amount * 100)
            if (!long.TryParse(amount, out var amountInVND))
            {
                return BadRequest(new { message = "Invalid amount format" });
            }
            var actualAmount = amountInVND / 100;

            // Extract userId from orderInfo or txnRef (you need to encode userId in the payment request)
            // For now, assuming orderInfo contains "UserId:{userId}"
            var userId = ExtractUserIdFromOrderInfo(orderInfo);
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { message = "Cannot determine user ID from payment" });
            }

            // Get or create wallet
            var wallet = await _walletService.GetWalletByUserIdAsync(userId);
            if (wallet == null)
            {
                wallet = new Wallet
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Balance = 0
                };
                await _walletService.CreateWalletAsync(wallet);
            }

            // Update wallet balance
            wallet.Balance += actualAmount;
            await _walletService.UpdateWalletAsync(wallet);

            // Create transaction record
            var transaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                WalletId = wallet.Id,
                Amount = actualAmount,
                Type = "Deposit",
                CreatedAt = DateTime.UtcNow,
                Description = $"VNPay deposit - TxnRef: {txnRef}, BankCode: {bankCode}, TransactionNo: {transactionNo}"
            };
            await _transactionService.CreateTransactionAsync(transaction);

            return Ok(new 
            { 
                success = true, 
                message = "Payment successful and wallet updated",
                walletBalance = wallet.Balance,
                transactionId = transaction.Id,
                amount = actualAmount,
                txnRef
            });
        }

        private string ExtractUserIdFromOrderInfo(string orderInfo)
        {
            // Expected format: "UserId:{userId}" or "UserId:{userId}|{additionalInfo}"
            if (string.IsNullOrEmpty(orderInfo))
                return string.Empty;

            if (orderInfo.StartsWith("UserId:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = orderInfo.Substring(7).Split('|');
                return parts[0].Trim();
            }

            return string.Empty;
        }
    }
}