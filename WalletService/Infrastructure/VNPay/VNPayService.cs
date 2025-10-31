using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WalletService.Application.DTOs;
using System.Threading.Tasks;

namespace WalletService.Infrastructure.VNPay
{
    public class VNPayService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<VNPayService> _logger;
        private readonly HttpClient _http;

        public VNPayService(IConfiguration config, ILogger<VNPayService> logger, HttpClient http)
        {
            _config = config;
            _logger = logger;
            _http = http;
        }

        /// <summary>
        /// Tạo URL thanh toán VNPay, tự động lấy ngrok public URL mới
        /// </summary>
        public async Task<VNPayResponseDTO> CreatePaymentUrlAsync(VNPayRequestDTO request)
        {
            var vnp_Url = _config["VNPay:Url"];
            var vnp_TmnCode = _config["VNPay:TmnCode"];
            var vnp_HashSecret = _config["VNPay:HashSecret"];
            var vnp_ReturnUrl = await GetNgrokUrlAsync(); // Lấy URL public mới từ ngrok

            var vnp_Amount = request.Amount;
            var vnp_TxnRef = Guid.NewGuid().ToString("N").Substring(0, 12);
            var vnp_OrderInfo = "Nap tien test";
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Windows
            var now = TimeZoneInfo.ConvertTime(DateTime.Now, vnTimeZone);

            var vnp_CreateDate = now.ToString("yyyyMMddHHmmss");
            var vnp_ExpireDate = now.AddMinutes(15).ToString("yyyyMMddHHmmss");
            var vnp_IpAddr = "127.0.0.1";
            var vnp_BankCode = "VNPAYQR";

            var inputData = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                {"vnp_Version", "2.1.0"},
                {"vnp_Command", "pay"},
                {"vnp_TmnCode", vnp_TmnCode},
                {"vnp_Amount", vnp_Amount.ToString()},
                {"vnp_CurrCode", "VND"},
                {"vnp_TxnRef", vnp_TxnRef},
                {"vnp_OrderInfo", vnp_OrderInfo},
                {"vnp_OrderType", "140000"},
                {"vnp_BankCode", vnp_BankCode},
                {"vnp_Locale", "vn"},
                {"vnp_ReturnUrl", vnp_ReturnUrl},
                {"vnp_IpAddr", vnp_IpAddr},
                {"vnp_CreateDate", vnp_CreateDate},
                {"vnp_ExpireDate", vnp_ExpireDate}
            };

            // Tạo signData
            var signData = string.Join("&", inputData
                            .Where(kv => !string.IsNullOrEmpty(kv.Value))
                            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                            .Select(kv => $"{kv.Key}={kv.Value}"));

            var vnp_SecureHash = HmacSHA512(vnp_HashSecret, signData);

            var query = string.Join("&", inputData.Select(kv => $"{kv.Key}={HttpUtility.UrlEncode(kv.Value)}"));
            var paymentUrl = $"{vnp_Url}?{query}&vnp_SecureHash={vnp_SecureHash}";

            _logger?.LogInformation("VNPay PaymentUrl: {paymentUrl}", paymentUrl);
            _logger?.LogInformation("VNPay signData: {signData}", signData);

            return new VNPayResponseDTO { PaymentUrl = paymentUrl };
        }

        /// <summary>
        /// Validate callback từ VNPay
        /// </summary>
        public bool ValidateCallback(Dictionary<string, string> callbackData)
        {
            if (!callbackData.TryGetValue("vnp_SecureHash", out var receivedHash) || string.IsNullOrEmpty(receivedHash))
            {
                _logger?.LogWarning("VNPay callback missing vnp_SecureHash.");
                return false;
            }

            callbackData.Remove("vnp_SecureHash");
            callbackData.Remove("vnp_SecureHashType");

            var signData = string.Join("&", callbackData
                                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                                .Select(kv => $"{kv.Key}={kv.Value}"));

            var secret = _config["VNPay:HashSecret"];
            var computedHash = HmacSHA512(secret, signData);

            _logger?.LogInformation("VNPay ValidateCallback signData: {signData}", signData);
            _logger?.LogInformation("VNPay computedHash: {computedHash}, receivedHash: {receivedHash}", computedHash, receivedHash);

            return string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase);
        }

        private string HmacSHA512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        /// <summary>
        /// Lấy URL public mới từ ngrok
        /// </summary>
        private async Task<string> GetNgrokUrlAsync()
        {
            try
            {
                var response = await _http.GetStringAsync("http://127.0.0.1:4040/api/tunnels");
                using var doc = JsonDocument.Parse(response);
                var tunnels = doc.RootElement.GetProperty("tunnels");
                foreach (var t in tunnels.EnumerateArray())
                {
                    if (t.GetProperty("proto").GetString() == "http")
                        return t.GetProperty("public_url").GetString() + "/api/vnpay/callback";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Lỗi lấy URL ngrok");
            }

            return _config["VNPay:ReturnUrl"]; // fallback
        }
    }
}
