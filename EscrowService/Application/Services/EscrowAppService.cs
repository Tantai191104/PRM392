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

        Task<List<EscrowResponseDto>> GetAllEscrowsAsync(EscrowFilterDto filters);

        Task<EscrowResponseDto?> GetEscrowByIdAsync(string id);
        Task<EscrowResponseDto?> GetEscrowByOrderIdAsync(string orderId);
        Task<List<EscrowResponseDto>> GetEscrowsByBuyerAsync(string buyerId);
        Task<List<EscrowResponseDto>> GetEscrowsBySellerAsync(string sellerId);
        Task<EscrowResponseDto> ReleaseEscrowAsync(string id, string userId, ReleaseEscrowDto dto, bool isAdminOrStaff = false);
        Task<EscrowResponseDto> RefundEscrowAsync(string id, string userId, RefundEscrowDto dto, bool isAdminOrStaff = false);
    }

    public class EscrowAppService : IEscrowAppService
    {
        private readonly IEscrowRepository _escrowRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IPaymentProvider _paymentProvider;
        private readonly EscrowSagaOrchestrator _sagaOrchestrator;
        // Đã loại bỏ IOrderServiceClient, không còn sử dụng
        private readonly ILogger<EscrowAppService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILoggerFactory _loggerFactory;

        public EscrowAppService(
            IEscrowRepository escrowRepo,
            IPaymentRepository paymentRepo,
            IPaymentProvider paymentProvider,
            EscrowSagaOrchestrator sagaOrchestrator,
            // Đã loại bỏ IOrderServiceClient
            ILogger<EscrowAppService> logger,
            IHttpClientFactory httpClientFactory,
            ILoggerFactory loggerFactory)
        {
            _escrowRepo = escrowRepo;
            _paymentRepo = paymentRepo;
            _paymentProvider = paymentProvider;
            _sagaOrchestrator = sagaOrchestrator;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _loggerFactory = loggerFactory;
        }
        public async Task<List<EscrowResponseDto>> GetAllEscrowsAsync(EscrowFilterDto filters)
        {
            var escrows = await _escrowRepo.GetAllAsync(
                status: filters.Status,
                buyerId: filters.BuyerId,
                sellerId: filters.SellerId,
                orderId: filters.OrderId
            );

            return escrows.Select(MapToDto).ToList();
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

        public async Task<EscrowResponseDto> ReleaseEscrowAsync(string id, string userId, ReleaseEscrowDto dto, bool isAdminOrStaff = false)
        {
            var escrow = await _escrowRepo.GetByIdAsync(id);
            if (escrow == null)
                throw new ArgumentException("Escrow not found");

            if (!isAdminOrStaff && escrow.SellerId != userId)
                throw new UnauthorizedAccessException("Only seller can release escrow");

            if (escrow.Status != EscrowStatus.HOLDING &&
                escrow.Status != EscrowStatus.CAPTURED)
            {
                throw new InvalidOperationException($"Cannot refund escrow with status {escrow.Status}");
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
        public async Task<EscrowResponseDto> RefundEscrowAsync(string id, string userId, RefundEscrowDto dto, bool isAdminOrStaff = false)
        {
            _logger.LogInformation("[RefundEscrow] Start refund for escrowId={EscrowId}, userId={UserId}", id, userId);
            var escrow = await _escrowRepo.GetByIdAsync(id);
            if (escrow == null)
            {
                _logger.LogWarning("[RefundEscrow] Escrow not found: {EscrowId}", id);
                throw new ArgumentException("Escrow not found");
            }

            _logger.LogInformation("[RefundEscrow] Escrow found. BuyerId={BuyerId}, Status={Status}", escrow.BuyerId, escrow.Status);

            // Bỏ kiểm tra trạng thái đơn hàng từ OrderService

            if (!isAdminOrStaff && escrow.BuyerId != userId)
            {
                _logger.LogWarning("[RefundEscrow] Unauthorized refund attempt. Escrow.BuyerId={BuyerId}, userId={UserId}", escrow.BuyerId, userId);
                throw new UnauthorizedAccessException("Only buyer can request refund");
            }

            if (escrow.Status != EscrowStatus.HOLDING)
            {
                _logger.LogWarning("[RefundEscrow] Invalid escrow status for refund. Status={Status}", escrow.Status);
                throw new InvalidOperationException($"Cannot refund escrow with status {escrow.Status}");
            }

            // Refund via payment provider
            if (escrow.Payment?.PaymentIntentId != null)
            {
                _logger.LogInformation("[RefundEscrow] Refunding payment. PaymentIntentId={PaymentIntentId}, Amount={Amount}", escrow.Payment.PaymentIntentId, escrow.AmountTotal);
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

            _logger.LogInformation("[RefundEscrow] Releasing money to buyer wallet. BuyerId={BuyerId}, Amount={Amount}", escrow.BuyerId, escrow.AmountTotal);
            var refundResult = await walletClient.ReleaseMoneyAsync(escrow.BuyerId, escrow.AmountTotal);
            if (!refundResult)
            {
                _logger.LogError("[RefundEscrow] Failed to release money to buyer wallet. BuyerId={BuyerId}, Amount={Amount}", escrow.BuyerId, escrow.AmountTotal);
                throw new InvalidOperationException("Không thể hoàn tiền vào ví người mua. Vui lòng thử lại.");
            }

            escrow.Status = EscrowStatus.REFUNDED;
            escrow.AddEvent(EscrowEventType.REFUNDED, dto.Reason, userId);

            await _escrowRepo.UpdateAsync(escrow);
            _logger.LogInformation("[RefundEscrow] Refunded escrow {EscrowId} to buyer {BuyerId}", id, escrow.BuyerId);

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
