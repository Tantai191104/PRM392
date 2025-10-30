using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Configuration;
using WalletService.Application.DTOs;

namespace WalletService.Infrastructure.VNPay
{
    public class VNPayService
    {
        private readonly IConfiguration _config;
        public VNPayService(IConfiguration config)
        {
            _config = config;
        }

        public VNPayResponseDTO CreatePaymentUrl(VNPayRequestDTO request)
        {
            var vnp_Url = _config["VNPay:Url"];
            var vnp_TmnCode = _config["VNPay:TmnCode"];
            var vnp_HashSecret = _config["VNPay:HashSecret"];
            var vnp_ReturnUrl = request.ReturnUrl ?? _config["VNPay:ReturnUrl"];

            var vnp_Amount = ((int)request.Amount * 100).ToString();
            var vnp_TxnRef = DateTime.Now.Ticks.ToString();

            var vnp_OrderInfo = string.IsNullOrEmpty(request.OrderInfo)
                ? $"UserId:{request.UserId}"
                : $"UserId:{request.UserId}|{request.OrderInfo}";

            var inputData = new SortedDictionary<string, string>
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
            var signData = string.Join("&", inputData.Select(x => x.Key + "=" + x.Value));

            // Tính chữ ký
            var vnp_SecureHash = HmacSHA512(vnp_HashSecret, signData);
            var paymentUrl = $"{vnp_Url}?{query}&vnp_SecureHash={vnp_SecureHash}";

            return new VNPayResponseDTO { PaymentUrl = paymentUrl };
        }

        public bool ValidateCallback(Dictionary<string, string> callbackData)
        {
            if (!callbackData.ContainsKey("vnp_SecureHash"))
                return false;

            var receivedHash = callbackData["vnp_SecureHash"];
            callbackData.Remove("vnp_SecureHash");
            callbackData.Remove("vnp_SecureHashType");

            var sortedData = new SortedDictionary<string, string>(callbackData);
            var signData = string.Join("&", sortedData.Select(x => x.Key + "=" + x.Value));

            var computedHash = HmacSHA512(_config["VNPay:HashSecret"], signData);

            return computedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase);
        }

        private string HmacSHA512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
