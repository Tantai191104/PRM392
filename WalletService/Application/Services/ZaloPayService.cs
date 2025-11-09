using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WalletService.Domain.Entities;

namespace WalletService.Application.Services
{
    public class ZaloPayService
    {
        private readonly string appId = "554";
        private readonly string key1 = "8NdU5pG5R2spGHGhyO99HN1OhD8IQJBn";
        private readonly string key2 = "uUfsWgfLkRLzq6W2uNXTCxrfxs51auny";
        private readonly string createOrderUrl = "https://sandbox.zalopay.com.vn/v001/tpe/createorder";
        private readonly string queryOrderUrl = "https://sandbox.zalopay.com.vn/v001/tpe/getstatusbyapptransid";

        private readonly WalletAppService _walletAppService;
        private readonly TransactionService _transactionService;

        public ZaloPayService(WalletAppService walletAppService, TransactionService transactionService)
        {
            _walletAppService = walletAppService;
            _transactionService = transactionService;
        }

        // ===================== CREATE ORDER =====================
        public async Task<(bool Success, string Error, string AppTransId, string OrderUrl)> CreateOrderAsync(
            long amount, string description, string callbackUrl, string userId, string redirectUrl = null)
        {
            string appTransId = $"{DateTime.Now:yyMMdd_HHmmssfff}_{new Random().Next(1000, 9999)}";
            string appUser = userId;
            long appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var embedData = JsonConvert.SerializeObject(new 
            { 
                redirecturl = redirectUrl ?? "" 
            });
            var items = JsonConvert.SerializeObject(new { });

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

            try
            {
                using var client = new HttpClient();
                var content = new FormUrlEncodedContent(param);
                var response = await client.PostAsync(createOrderUrl, content);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString)!;

                if (result.TryGetValue("returncode", out var rc) && Convert.ToInt32(rc) == 1 &&
                    result.TryGetValue("orderurl", out var orderUrlObj))
                {
                    return (true, null, appTransId, orderUrlObj.ToString());
                }

                return (false, result.ContainsKey("returnmessage") ? result["returnmessage"].ToString() : "Unknown error", null, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null, null);
            }
        }

        // ===================== QUERY ORDER STATUS =====================
        public async Task<Dictionary<string, object>> QueryOrderStatusAsync(string appTransId)
        {
            string data = $"{appId}|{appTransId}|{key1}";
            string mac = ComputeHmacSHA256(data, key1);

            var param = new Dictionary<string, string>
            {
                { "appid", appId },
                { "apptransid", appTransId },
                { "mac", mac }
            };

            try
            {
                using var client = new HttpClient();
                var content = new FormUrlEncodedContent(param);
                var response = await client.PostAsync(queryOrderUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[QueryOrderStatus] Response: {responseString}");

                var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString)!;
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[QueryOrderStatus] Error: {ex.Message}");
                return new Dictionary<string, object> 
                { 
                    { "returncode", -1 }, 
                    { "returnmessage", ex.Message } 
                };
            }
        }

        // ===================== CHECK & CREDIT ORDER =====================
        public async Task<(bool Success, string Message, long Amount)> CheckAndCreditOrderAsync(string appTransId, string userId)
        {
            var status = await QueryOrderStatusAsync(appTransId);

            // Check if query was successful
            if (!status.TryGetValue("returncode", out var rcObj) || Convert.ToInt32(rcObj) != 1)
            {
                string message = status.TryGetValue("returnmessage", out var msgObj) 
                    ? msgObj.ToString() ?? "Query failed" 
                    : "Query failed";
                return (false, message, 0);
            }

            // Check transaction status (1 = success, 2 = failed, 3 = pending)
            if (!status.TryGetValue("zptransid", out var zpTransIdObj))
            {
                return (false, "Transaction not found or still pending", 0);
            }

            // Get amount
            long amount = status.TryGetValue("amount", out var amtObj) ? Convert.ToInt64(amtObj) : 0;
            if (amount <= 0)
            {
                return (false, "Invalid amount", 0);
            }

            // Credit wallet
            var wallet = await _walletAppService.GetWalletByUserIdAsync(userId);
            if (wallet == null)
            {
                return (false, "Wallet not found", 0);
            }

            wallet.Balance += amount;
            await _walletAppService.UpdateWalletAsync(wallet);

            // Create transaction record
            var transaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                WalletId = wallet.Id,
                Amount = amount,
                Type = "Deposit",
                Description = $"ZaloPay deposit: {appTransId}",
                CreatedAt = DateTime.UtcNow
            };
            await _transactionService.CreateTransactionAsync(transaction);

            return (true, "Payment successful", amount);
        }

        // ===================== HMAC HELPER =====================
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
