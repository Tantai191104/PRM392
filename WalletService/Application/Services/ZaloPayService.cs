#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WalletService.Application.Services
{
    public class ZaloPayService
    {
        private const string AppId = "554";
        private const string Key1 = "8NdU5pG5R2spGHGhyO99HN1OhD8IQJBn";
        private const string CreateOrderUrl = "https://sandbox.zalopay.com.vn/v001/tpe/createorder";
        private const string QueryOrderUrl = "https://sandbox.zalopay.com.vn/v001/tpe/getstatusbyapptransid";

        public async Task<(bool Success, string? Error, string? AppTransId, string? OrderUrl)> CreateOrderAsync(
            long amount,
            string description,
            string callbackUrl,
            string userId)
        {
            string appTransId = $"{DateTime.Now:yyMMdd}_{DateTime.Now:HHmmss}_{Random.Shared.Next(100, 1000)}";
            long appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string appUser = userId;

            string embedData = JsonConvert.SerializeObject(new { });
            string items = JsonConvert.SerializeObject(new { });

            string data = $"{AppId}|{appTransId}|{appUser}|{amount}|{appTime}|{embedData}|{items}";
            string mac = ComputeHmacSHA256(data, Key1);

            var param = new Dictionary<string, string>
            {
                ["appid"] = AppId,
                ["apptransid"] = appTransId,
                ["appuser"] = appUser,
                ["amount"] = amount.ToString(),
                ["apptime"] = appTime.ToString(),
                ["embeddata"] = embedData,
                ["item"] = items,
                ["description"] = description,
                ["callbackurl"] = callbackUrl,
                ["mac"] = mac
            };

            try
            {
                using var client = new HttpClient();
                using var content = new FormUrlEncodedContent(param);
                using var response = await client.PostAsync(CreateOrderUrl, content);

                if (!response.IsSuccessStatusCode)
                    return (false, $"HTTP {(int)response.StatusCode}", null, null);

                string responseString = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, object?>>(responseString)
                             ?? new Dictionary<string, object?>();

                if (result.TryGetValue("returncode", out var rcObj) && rcObj is long rc && rc == 1 &&
                    result.TryGetValue("orderurl", out var orderUrlObj))
                {
                    return (true, null, appTransId, orderUrlObj?.ToString());
                }

                string msg = result.TryGetValue("returnmessage", out var rm)
                             ? rm?.ToString() ?? "Unknown error"
                             : "Unknown error";

                return (false, msg, null, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null, null);
            }
        }

        public async Task<Dictionary<string, object?>> QueryOrderStatusAsync(string appTransId)
        {
            var param = new Dictionary<string, string>
            {
                ["appid"] = AppId,
                ["apptransid"] = appTransId
            };

            string data = $"{AppId}|{appTransId}|{Key1}";
            param["mac"] = ComputeHmacSHA256(data, Key1);

            using var client = new HttpClient();
            using var content = new FormUrlEncodedContent(param);
            using var response = await client.PostAsync(QueryOrderUrl, content);
            string json = await response.Content.ReadAsStringAsync();

            var outer = JsonConvert.DeserializeObject<Dictionary<string, object?>>(json)
                        ?? new Dictionary<string, object?>();

            if (outer.TryGetValue("data", out var dataObj) && dataObj is string dataStr)
            {
                try
                {
                    var inner = JsonConvert.DeserializeObject<Dictionary<string, object?>>(dataStr);
                    if (inner != null) return inner;
                }
                catch { }
            }

            return outer;
        }

        private static string ComputeHmacSHA256(string message, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            using var hmac = new HMACSHA256(keyBytes);
            byte[] hash = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}