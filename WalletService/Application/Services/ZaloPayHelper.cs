using System.Security.Cryptography;
using System.Text;

namespace WalletService.Application.Services
{
    public static class ZaloPayHelper
    {
        public static string ComputeHmacSHA256(string key, string data)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var sb = new StringBuilder();
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
