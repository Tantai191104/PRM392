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

        /// <summary>
        /// Tạo URL thanh toán VNPay
        /// </summary>
        public VNPayResponseDTO CreatePaymentUrl(VNPayRequestDTO request)
        {
            // Lấy config
            var vnp_Url = _config["VNPay:Url"];
            var vnp_TmnCode = _config["VNPay:TmnCode"];
            var vnp_HashSecret = _config["VNPay:HashSecret"];
            var vnp_ReturnUrl = string.IsNullOrEmpty(request.ReturnUrl) || request.ReturnUrl == "string"
                ? _config["VNPay:ReturnUrl"]
                : request.ReturnUrl;

            // Chuẩn hóa số tiền (VND * 100)
            var vnp_Amount = Convert.ToInt64(Math.Round(request.Amount * 100)).ToString();
            var vnp_TxnRef = DateTime.Now.Ticks.ToString();

            // Chuẩn hóa OrderInfo
            var vnp_OrderInfo = string.IsNullOrEmpty(request.OrderInfo)
                ? $"UserId:{request.UserId}"
                : $"UserId:{request.UserId}|{request.OrderInfo}";

            // Dữ liệu thanh toán
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

            // Tạo signData (không encode)
            var signData = string.Join("&",
                inputData.Where(kv => !string.IsNullOrEmpty(kv.Value))
                         .Select(kv => $"{kv.Key}={kv.Value}"));

            // Tính chữ ký HMACSHA512
            var vnp_SecureHash = HmacSHA512(vnp_HashSecret, signData);

            // Build URL (encode giá trị)
            var query = string.Join("&",
                inputData.Select(kv => $"{kv.Key}={HttpUtility.UrlEncode(kv.Value)}"));

            var paymentUrl = $"{vnp_Url}?{query}&vnp_SecureHash={vnp_SecureHash}";

            // Log để debug
            _logger?.LogInformation("VNPay PaymentUrl: {paymentUrl}", paymentUrl);
            _logger?.LogInformation("VNPay signData for hash: {signData}", signData);
            _logger?.LogInformation("VNPay computed hash: {vnp_SecureHash}", vnp_SecureHash);

            return new VNPayResponseDTO { PaymentUrl = paymentUrl };
        }

        /// <summary>
        /// Xác thực callback VNPay
        /// </summary>
        public bool ValidateCallback(Dictionary<string, string> callbackData)
        {
            _logger?.LogInformation("VNPay callback raw data: {@callbackData}", callbackData);

            if (!callbackData.TryGetValue("vnp_SecureHash", out var receivedHash) || string.IsNullOrEmpty(receivedHash))
            {
                _logger?.LogWarning("VNPay callback missing vnp_SecureHash.");
                return false;
            }

            // Remove hash fields
            callbackData.Remove("vnp_SecureHash");
            callbackData.Remove("vnp_SecureHashType");

            // Tạo signData từ callback (sắp xếp ASCII, không encode)
            var signData = string.Join("&",
                callbackData.Where(kv => !string.IsNullOrEmpty(kv.Value))
                            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                            .Select(kv => $"{kv.Key}={kv.Value}"));

            var secret = _config["VNPay:HashSecret"];
            if (string.IsNullOrEmpty(secret))
            {
                _logger?.LogError("VNPay HashSecret is empty in configuration.");
                return false;
            }

            var computedHash = HmacSHA512(secret, signData);

            _logger?.LogInformation("VNPay ValidateCallback signData: {signData}", signData);
            _logger?.LogInformation("VNPay computedHash: {computedHash}, receivedHash: {receivedHash}", computedHash, receivedHash);

            var isValid = string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger?.LogWarning("VNPay signature mismatch. TxnRef: {txnRef}. SignData: {signData}. ComputedHash: {computedHash}. ReceivedHash: {receivedHash}.",
                    callbackData.TryGetValue("vnp_TxnRef", out var r) ? r : string.Empty,
                    signData,
                    computedHash,
                    receivedHash);
            }
            else
            {
                _logger?.LogInformation("VNPay signature validated successfully. TxnRef: {txnRef}",
                    callbackData.TryGetValue("vnp_TxnRef", out var r2) ? r2 : string.Empty);
            }

            return isValid;
        }

        /// <summary>
        /// HMACSHA512
        /// </summary>
        private string HmacSHA512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
