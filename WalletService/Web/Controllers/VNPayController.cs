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
using Microsoft.Extensions.Logging;

namespace WalletService.Web.Controllers
{
    [ApiController]
    [Route("api/vnpay")]
    public class VNPayController : ControllerBase
    {
        private readonly IVnPayService _vnpayService;
        private readonly WalletAppService _walletService;
        private readonly TransactionService _transactionService;
        private readonly ILogger<VNPayController> _logger;

        public VNPayController(
            IVnPayService vnpayService,
            WalletAppService walletService,
            TransactionService transactionService,
            ILogger<VNPayController> logger)
        {
            _vnpayService = vnpayService;
            _walletService = walletService;
            _transactionService = transactionService;
            _logger = logger;
        }

        /// <summary>
        /// Create VNPay payment URL
        /// </summary>
        [HttpPost("create-payment-url")]
        [ProducesResponseType(typeof(VNPayResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePaymentUrl([FromBody] VNPayRequestDTO request)
        {
            if (request == null || request.Amount <= 0)
            {
                return BadRequest(new { message = "Invalid payment request" });
            }

            // Sử dụng phương thức async của VNPayService
            var paymentInfo = new PaymentInformationModel
            {
                Amount = (double)request.Amount,
                Name = request.UserId, // or set to a default value
                OrderDescription = request.OrderDescription ?? string.Empty,
                OrderType = request.OrderType ?? "140000"
            };
            var response = await _vnpayService.CreatePaymentUrlAsync(paymentInfo, HttpContext);
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
            var callbackData = Request.Query;

            // Log để debug
            _logger?.LogInformation("VNPay callback HTTP {method} received. QueryString: {qs}. Content-Type: {ct}",
                Request.Method,
                Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty,
                Request.ContentType ?? string.Empty);

            // If no query params and form content type, fallback to form
            // Only use query params for callback validation
            // If you need to support form data, handle it separately as a dictionary
            // Remove any attempt to cast IFormCollection to IQueryCollection

            // Validate signature
            if (!_vnpayService.ValidateCallback(callbackData))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid signature"
                });
            }

            var responseCode = callbackData.ContainsKey("vnp_ResponseCode") ? callbackData["vnp_ResponseCode"].ToString() : "";
            var txnRef = callbackData.ContainsKey("vnp_TxnRef") ? callbackData["vnp_TxnRef"].ToString() : "";
            var amount = callbackData.ContainsKey("vnp_Amount") ? callbackData["vnp_Amount"].ToString() : "0";
            var bankCode = callbackData.ContainsKey("vnp_BankCode") ? callbackData["vnp_BankCode"].ToString() : "";
            var transactionNo = callbackData.ContainsKey("vnp_TransactionNo") ? callbackData["vnp_TransactionNo"].ToString() : "";
            var orderInfo = callbackData.ContainsKey("vnp_OrderInfo") ? callbackData["vnp_OrderInfo"].ToString() : "";

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

            if (!long.TryParse(amount, out var amountInVND))
            {
                return BadRequest(new { message = "Invalid amount format" });
            }
            var actualAmount = amountInVND / 100;

            var userId = ExtractUserIdFromOrderInfo(orderInfo);
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { message = "Cannot determine user ID from payment" });
            }

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

            wallet.Balance += actualAmount;
            await _walletService.UpdateWalletAsync(wallet);

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
