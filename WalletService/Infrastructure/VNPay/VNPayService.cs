using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WalletService.Application.DTOs;

namespace WalletService.Infrastructure.VNPay
{
    public class VNPayService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<VNPayService> _logger;

        public VNPayService(IConfiguration config, ILogger<VNPayService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public VNPayResponseDTO CreatePaymentUrl(VNPayRequestDTO request)
        {
            var vnp_Url = _config["VNPay:Url"];
            var vnp_TmnCode = _config["VNPay:TmnCode"];
            var vnp_HashSecret = _config["VNPay:HashSecret"];
            var vnp_ReturnUrl = request.ReturnUrl ?? _config["VNPay:ReturnUrl"];

            // VNPay expects amount in VND multiplied by 100 (no decimals). Use rounding then convert to long.
            var vnp_Amount = Convert.ToInt64(Math.Round(request.Amount * 100)).ToString();
            var vnp_TxnRef = DateTime.Now.Ticks.ToString();

            var vnp_OrderInfo = string.IsNullOrEmpty(request.OrderInfo)
                ? $"UserId:{request.UserId}"
                : $"UserId:{request.UserId}|{request.OrderInfo}";

            // Use ordinal comparer to ensure deterministic ordering matching callback validation
            var inputData = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                {"vnp_Version", "2.1.0"},
                {"vnp_Command", "pay"},
                {"vnp_TmnCode", vnp_TmnCode},
                {"vnp_Amount", vnp_Amount},
                {"vnp_CurrCode", "VND"},
                {"vnp_TxnRef", vnp_TxnRef},
                {"vnp_OrderInfo", vnp_OrderInfo},
                {"vnp_OrderType", "other"},
                {"vnp_Locale", "vn"},
                {"vnp_ReturnUrl", vnp_ReturnUrl},
                {"vnp_IpAddr", "127.0.0.1"},
                {"vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")}
            };

            // Build query string
            var query = string.Join("&", inputData.Select(x => x.Key + "=" + HttpUtility.UrlEncode(x.Value)));

            // Build signData using same rules as callback validation: ordinal ordering, exclude empty values, raw (not URL-encoded) values
            var filtered = inputData.Where(kv => !string.IsNullOrEmpty(kv.Value)).ToList();
            var signData = string.Join("&", filtered.Select(x => x.Key + "=" + x.Value));

            // Tính chữ ký
            var vnp_SecureHash = HmacSHA512(vnp_HashSecret, signData);
            var paymentUrl = $"{vnp_Url}?{query}&vnp_SecureHash={vnp_SecureHash}";

            return new VNPayResponseDTO { PaymentUrl = paymentUrl };
        }

        public bool ValidateCallback(Dictionary<string, string> callbackData)
        {
            if (!callbackData.TryGetValue("vnp_SecureHash", out var receivedHash) || string.IsNullOrEmpty(receivedHash))
            {
                _logger?.LogWarning("VNPay callback missing vnp_SecureHash.");
                return false;
            }

            // Remove hash fields before building sign string
            callbackData.Remove("vnp_SecureHash");
            callbackData.Remove("vnp_SecureHashType");

            // Ensure deterministic ordering (ordinal) and exclude empty values
            var sortedData = new SortedDictionary<string, string>(callbackData, StringComparer.Ordinal);
            var filtered = sortedData.Where(kv => !string.IsNullOrEmpty(kv.Value)).ToList();
            var signData = string.Join("&", filtered.Select(x => x.Key + "=" + x.Value));

            var secret = _config["VNPay:HashSecret"];
            if (string.IsNullOrEmpty(secret))
            {
                _logger?.LogError("VNPay HashSecret is empty in configuration. Cannot validate signature.");
                return false;
            }

            var computedHash = HmacSHA512(secret, signData);

            var ok = computedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase);
            if (!ok)
            {
                _logger?.LogWarning("VNPay signature mismatch. TxnRef: {txnRef}. SignData: {signData}. ComputedHash: {computedHash}. ReceivedHash: {receivedHash}.",
                    sortedData.TryGetValue("vnp_TxnRef", out var r) ? r : string.Empty,
                    signData,
                    computedHash,
                    receivedHash);
            }
            else
            {
                _logger?.LogInformation("VNPay signature validated successfully. TxnRef: {txnRef}",
                    sortedData.TryGetValue("vnp_TxnRef", out var r2) ? r2 : string.Empty);
            }

            return ok;
        }

        private string HmacSHA512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
