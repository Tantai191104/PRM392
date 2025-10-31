using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;        // HttpContext, IQueryCollection
using Microsoft.Extensions.Configuration; // IConfiguration
using System.Collections.Generic;        // SortedList<,>, IComparer<>
using WalletService.Application.DTOs;    // PaymentInformationModel, PaymentResponseModel
namespace WalletService.Infrastructure.VNPay
{
    public interface IVnPayService
    {
    string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
    Task<string> CreatePaymentUrlAsync(PaymentInformationModel model, HttpContext context);
    PaymentResponseModel PaymentExecute(IQueryCollection collections);
    bool ValidateCallback(IQueryCollection callbackData);

    }
}