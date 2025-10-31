using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WalletService.Application.Services
{
    public class ZaloPayService
    {
        private readonly string appId = "554"; // sandbox AppId
        private readonly string key1 = "8NdU5pG5R2spGHGhyO99HN1OhD8IQJBn";
        private readonly string createOrderUrl = "https://sandbox.zalopay.com.vn/v001/tpe/createorder";

        public async Task<Dictionary<string, string>> CreateOrderAsync(long amount, string description, string callbackUrl)
        {
            string appTransId = $"{DateTime.Now:yyMMdd_HHmmssfff}_{new Random().Next(1000, 9999)}";
            string appUser = "user123";
            long appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var embedData = JsonConvert.SerializeObject(new { });
            var items = JsonConvert.SerializeObject(new { });

            // Tạo MAC theo đúng docs: appid|apptransid|appuser|amount|apptime|embeddata|item
            string data = $"{appId}|{appTransId}|{appUser}|{amount}|{appTime}|{embedData}|{items}";
            string mac = ComputeHmacSHA256(data, key1);

            var param = new Dictionary<string, string>
            {
                { "appid", appId },
                { "apptransid", appTransId },
                { "appuser", appUser },
                { "amount", amount.ToString() },
                { "apptime", appTime.ToString() },
                { "embeddata", embedData },
                { "item", items },
                { "description", description },
                { "callbackurl", callbackUrl },
                { "mac", mac }
            };

            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(param);
            var response = await client.PostAsync(createOrderUrl, content);

            response.EnsureSuccessStatusCode();

            // Trả về Dictionary để dễ dùng (zptranstoken, orderurl, returncode...)
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString)!;
        }

        private string ComputeHmacSHA256(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
