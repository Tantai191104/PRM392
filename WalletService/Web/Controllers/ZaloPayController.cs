using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WalletService.Application.Services;

namespace WalletService.Web.Controllers
{
    [ApiController]
    [Route("api/zalopay")]
    public class ZaloPayController : ControllerBase
    {
        private readonly ZaloPayService _zalopayService;

        public ZaloPayController(ZaloPayService zalopayService)
        {
            _zalopayService = zalopayService;
        }

        public class CreateOrderDto
        {
            public long Amount { get; set; }
            public string Description { get; set; } = string.Empty;
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (dto.Amount <= 0 || string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest("Amount and Description are required");

            string callbackUrl = "https://9d500abdec62.ngrok-free.app/api/zalopay/callback";

            // Gọi service tạo order
            var result = await _zalopayService.CreateOrderAsync(dto.Amount, dto.Description, callbackUrl);

            return Ok(result); // Trả về zptranstoken, orderurl, returncode, returnmessage
        }

        [HttpPost("callback")]
        public IActionResult Callback([FromBody] JsonElement body)
        {
            string returnCode = body.GetProperty("returncode").GetString() ?? "";
            string appTransId = body.GetProperty("apptransid").GetString() ?? "";
            long amount = body.GetProperty("amount").GetInt64();
            string mac = body.GetProperty("mac").GetString() ?? "";

            if (returnCode == "1")
                Console.WriteLine($"Payment success: {appTransId}, amount: {amount}");
            else
                Console.WriteLine($"Payment failed: {appTransId}");

            // ZaloPay docs yêu cầu trả về kiểu này
            return Ok(new { returncode = 1, returnmessage = "OK" });
        }
    }
}
