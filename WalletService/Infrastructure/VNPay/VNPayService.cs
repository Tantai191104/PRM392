using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using WalletService.Infrastructure.VNPayLibrary;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using WalletService.Application.DTOs;
using System;
using System.Threading.Tasks;

namespace WalletService.Infrastructure.VNPay
{
    public class VNPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VNPayService> _logger;

        public VNPayService(IConfiguration configuration, ILogger<VNPayService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string CreatePaymentUrl(PaymentInformationModel model, HttpContext context)
        {
            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(_configuration["TimeZoneId"]);
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var tick = DateTime.Now.Ticks.ToString();
            var vnPay = new WalletService.Infrastructure.VNPayLibrary.VNPayLibrary();
            var urlCallBack = _configuration["PaymentCallBack:ReturnUrl"];

            vnPay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"]);
            vnPay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"]);
            vnPay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"]);
            vnPay.AddRequestData("vnp_Amount", ((int)model.Amount * 100).ToString());
            vnPay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            vnPay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"]);
            vnPay.AddRequestData("vnp_IpAddr", vnPay.GetIpAddress(context));
            vnPay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"]);
            vnPay.AddRequestData("vnp_OrderInfo", $"Nap tien vao vi {model.Amount}");
            vnPay.AddRequestData("vnp_OrderType", "140000");
            vnPay.AddRequestData("vnp_ReturnUrl", urlCallBack);
            vnPay.AddRequestData("vnp_TxnRef", tick);

            // Log each key-value pair in request data for easier debugging
            var requestData = vnPay.GetType().GetField("_requestData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(vnPay) as SortedList<string, string>;
            if (requestData != null)
            {
                foreach (var kvp in requestData)
                {
                    _logger?.LogInformation($"VNPay Request Field: {kvp.Key} = {kvp.Value}");
                }
            }

            var paymentUrl = vnPay.CreateRequestUrl(_configuration["Vnpay:BaseUrl"], _configuration["Vnpay:HashSecret"]);
            _logger?.LogInformation("VNPay Payment URL: {url}", paymentUrl);
            return paymentUrl;
        }

        public Task<string> CreatePaymentUrlAsync(PaymentInformationModel model, HttpContext context)
        {
            // No actual async work, but matching controller signature
            return Task.FromResult(CreatePaymentUrl(model, context));
        }

        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var vnPay = new WalletService.Infrastructure.VNPayLibrary.VNPayLibrary();
            var response = vnPay.GetFullResponseData(collections, _configuration["Vnpay:HashSecret"]);

            // Log all callback data received from VNPay
            foreach (var key in collections.Keys)
            {
                var value = collections[key];
                _logger?.LogInformation($"VNPay Callback Field: {key} = {value}");
            }

            // Log the parsed response object
            _logger?.LogInformation("VNPay Parsed Response: {@response}", response);
            return response;
        }

        public bool ValidateCallback(IQueryCollection callbackData)
        {
            var response = PaymentExecute(callbackData);
            return response.Success;
        }
    }

}