using EscrowService.Application.DTOs;
using EscrowService.Application.Saga;
using EscrowService.Domain.Entities;
using EscrowService.Infrastructure.Providers;
using EscrowService.Infrastructure.Repositories;
using EscrowService.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;

namespace EscrowService.Application.Services
{
    public interface IEscrowAppService
    {
        Task<EscrowResponseDto> CreateEscrowAsync(CreateEscrowDto dto, string buyerId);
        Task<EscrowResponseDto?> GetEscrowByIdAsync(string id);
        Task<EscrowResponseDto?> GetEscrowByOrderIdAsync(string orderId);
        Task<List<EscrowResponseDto>> GetEscrowsByBuyerAsync(string buyerId);
        Task<List<EscrowResponseDto>> GetEscrowsBySellerAsync(string sellerId);
        Task<EscrowResponseDto> ReleaseEscrowAsync(string id, string userId, ReleaseEscrowDto dto);
        Task<EscrowResponseDto> RefundEscrowAsync(string id, string userId, RefundEscrowDto dto);
    }

    public class EscrowAppService : IEscrowAppService
    {
        private readonly IEscrowRepository _escrowRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IPaymentProvider _paymentProvider;
        private readonly EscrowSagaOrchestrator _sagaOrchestrator;
        private readonly IOrderServiceClient _orderServiceClient;
        private readonly ILogger<EscrowAppService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILoggerFactory _loggerFactory;

        public EscrowAppService(
            IEscrowRepository escrowRepo,
            IPaymentRepository paymentRepo,
            IPaymentProvider paymentProvider,
            EscrowSagaOrchestrator sagaOrchestrator,
            IOrderServiceClient orderServiceClient,
            ILogger<EscrowAppService> logger,
            IHttpClientFactory httpClientFactory,
            ILoggerFactory loggerFactory)
        {
            _escrowRepo = escrowRepo;
            _paymentRepo = paymentRepo;
            _paymentProvider = paymentProvider;
            _sagaOrchestrator = sagaOrchestrator;
            _orderServiceClient = orderServiceClient;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _loggerFactory = loggerFactory;
        }

        public async Task<EscrowResponseDto> CreateEscrowAsync(CreateEscrowDto dto, string buyerId)
        {
            var context = new SagaContext
            {
                BuyerId = buyerId,
                SellerId = dto.SellerId,
                ProductId = dto.ProductId,
                Amount = dto.Amount,
                OrderId = dto.OrderId
            };

            var result = await _sagaOrchestrator.ExecuteCreateEscrowSagaAsync(context);

            if (!result.Success)
                throw new InvalidOperationException($"Failed to create escrow: {result.ErrorMessage}");

            var escrow = await _escrowRepo.GetByIdAsync(result.EscrowId!);
            if (escrow == null)
                throw new InvalidOperationException("Escrow was created but not found");

            return MapToDto(escrow);
        }

        public async Task<EscrowResponseDto?> GetEscrowByIdAsync(string id)
        {
            var escrow = await _escrowRepo.GetByIdAsync(id);
            return escrow != null ? MapToDto(escrow) : null;
        }

        public async Task<EscrowResponseDto?> GetEscrowByOrderIdAsync(string orderId)
        {
            var escrow = await _escrowRepo.GetByOrderIdAsync(orderId);
            return escrow != null ? MapToDto(escrow) : null;
        }

        public async Task<List<EscrowResponseDto>> GetEscrowsByBuyerAsync(string buyerId)
        {
            var escrows = await _escrowRepo.GetByBuyerIdAsync(buyerId);
            return escrows.Select(MapToDto).ToList();
        }

        public async Task<List<EscrowResponseDto>> GetEscrowsBySellerAsync(string sellerId)
        {
            var escrows = await _escrowRepo.GetBySellerIdAsync(sellerId);
            return escrows.Select(MapToDto).ToList();
        }

        public async Task<EscrowResponseDto> ReleaseEscrowAsync(string id, string userId, ReleaseEscrowDto dto)
        {
            var escrow = await _escrowRepo.GetByIdAsync(id);
            if (escrow == null)
                throw new ArgumentException("Escrow not found");

            if (escrow.SellerId != userId)
                throw new UnauthorizedAccessException("Only seller can release escrow");

            if (escrow.Status != EscrowStatus.HOLDING)
                throw new InvalidOperationException($"Cannot release escrow with status {escrow.Status}");

            // Check order status
            if (!string.IsNullOrEmpty(escrow.OrderId))
            {
                var orderStatus = await GetOrderStatusFromOrderServiceAsync(escrow.OrderId);
                if (orderStatus != "Delivered")
                    throw new InvalidOperationException($"Cannot release escrow: order {escrow.OrderId} status is {orderStatus}, must be Delivered.");
            }

            // Transfer money via WalletService
            var httpClient = _httpClientFactory.CreateClient();
            var walletLogger = _loggerFactory.CreateLogger<WalletServiceClient>();
            var walletClient = new WalletServiceClient(httpClient, walletLogger);

            var transferResult = await walletClient.TransferAsync(escrow.BuyerId, escrow.SellerId, escrow.AmountTotal);
            if (!transferResult)
                throw new InvalidOperationException("Không thể chuyển tiền cho người bán. Vui lòng thử lại.");

            escrow.Status = EscrowStatus.RELEASED;
            escrow.Payout = new PayoutInfo
            {
                SellerAccountId = escrow.SellerId,
                PayoutStatus = "COMPLETED",
                PayoutAt = DateTime.UtcNow
            };
            escrow.AddEvent(EscrowEventType.RELEASED, dto.Reason ?? "Buyer confirmed delivery", userId);

            await _escrowRepo.UpdateAsync(escrow);
            _logger.LogInformation("Released escrow {EscrowId} to seller {SellerId}", id, escrow.SellerId);

            return MapToDto(escrow);
        }

        private async Task<string> GetOrderStatusFromOrderServiceAsync(string orderId)
        {
            var status = await _orderServiceClient.GetOrderStatusAsync(orderId);
            if (string.IsNullOrEmpty(status))
                throw new InvalidOperationException($"Cannot get order status from OrderService for order {orderId}");
            return status;
        }

        public async Task<EscrowResponseDto> RefundEscrowAsync(string id, string userId, RefundEscrowDto dto)
        {
            var escrow = await _escrowRepo.GetByIdAsync(id);
            if (escrow == null)
                throw new ArgumentException("Escrow not found");

            if (!string.IsNullOrEmpty(escrow.OrderId))
            {
                var orderStatus = await GetOrderStatusFromOrderServiceAsync(escrow.OrderId);
                if (orderStatus != "Returned")
                    throw new InvalidOperationException($"Cannot refund escrow: order {escrow.OrderId} status is {orderStatus}, must be Returned.");
            }

            if (escrow.BuyerId != userId)
                throw new UnauthorizedAccessException("Only buyer can request refund");

            if (escrow.Status != EscrowStatus.HOLDING)
                throw new InvalidOperationException($"Cannot refund escrow with status {escrow.Status}");

            // Refund via payment provider
            if (escrow.Payment?.PaymentIntentId != null)
            {
                await _paymentProvider.RefundAsync(escrow.Payment.PaymentIntentId, escrow.AmountTotal);

                var payment = new Payment
                {
                    EscrowId = id,
                    Provider = "Mock",
                    IntentId = escrow.Payment.PaymentIntentId,
                    Action = PaymentAction.REFUND,
                    Amount = escrow.AmountTotal,
                    Currency = escrow.Currency,
                    Status = PaymentStatus.SUCCEEDED
                };
                await _paymentRepo.CreateAsync(payment);
                escrow.Payment.RefundedAt = DateTime.UtcNow;
            }

            // Refund to buyer wallet
            var httpClient = _httpClientFactory.CreateClient();
            var walletLogger = _loggerFactory.CreateLogger<WalletServiceClient>();
            var walletClient = new WalletServiceClient(httpClient, walletLogger);

            var refundResult = await walletClient.ReleaseMoneyAsync(escrow.BuyerId, escrow.AmountTotal);
            if (!refundResult)
                throw new InvalidOperationException("Không thể hoàn tiền vào ví người mua. Vui lòng thử lại.");

            escrow.Status = EscrowStatus.REFUNDED;
            escrow.AddEvent(EscrowEventType.REFUNDED, dto.Reason, userId);

            await _escrowRepo.UpdateAsync(escrow);
            _logger.LogInformation("Refunded escrow {EscrowId} to buyer {BuyerId}", id, escrow.BuyerId);

            return MapToDto(escrow);
        }

        private EscrowResponseDto MapToDto(Escrow escrow)
        {
            return new EscrowResponseDto
            {
                Id = escrow.Id,
                OrderId = escrow.OrderId,
                ProductId = escrow.ProductId,
                BuyerId = escrow.BuyerId,
                SellerId = escrow.SellerId,
                Status = escrow.Status.ToString(),
                AmountTotal = escrow.AmountTotal,
                AmountHold = escrow.AmountHold,
                Currency = escrow.Currency,
                Payment = escrow.Payment,
                Events = escrow.Events,
                CreatedAt = escrow.CreatedAt,
                UpdatedAt = escrow.UpdatedAt
            };
        }
    }
}
