using EscrowService.Domain.Entities;
using EscrowService.Infrastructure.Providers;
using EscrowService.Infrastructure.Repositories;

namespace EscrowService.Application.Saga.Steps
{
    public class AuthorizePaymentStep : ISagaStep
    {
        private readonly IPaymentProvider _paymentProvider;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IEscrowRepository _escrowRepo;
        private readonly ILogger<AuthorizePaymentStep> _logger;

        public string StepName => "AuthorizePayment";

        public AuthorizePaymentStep(
            IPaymentProvider paymentProvider,
            IPaymentRepository paymentRepo,
            IEscrowRepository escrowRepo,
            ILogger<AuthorizePaymentStep> logger)
        {
            _paymentProvider = paymentProvider;
            _paymentRepo = paymentRepo;
            _escrowRepo = escrowRepo;
            _logger = logger;
        }

        public async Task<SagaStepResult> ExecuteAsync(SagaContext context)
        {
            try
            {
                // Authorize payment (hold funds)
                var intentId = await _paymentProvider.AuthorizeAsync(context.BuyerId, context.Amount);

                // Record payment
                var payment = new Payment
                {
                    EscrowId = context.EscrowId,
                    Provider = "Mock",
                    IntentId = intentId,
                    Action = PaymentAction.AUTHORIZE,
                    Amount = context.Amount,
                    Status = PaymentStatus.SUCCEEDED
                };

                await _paymentRepo.CreateAsync(payment);

                // Update escrow
                var escrow = await _escrowRepo.GetByIdAsync(context.EscrowId);
                if (escrow != null)
                {
                    escrow.Status = EscrowStatus.AUTHORIZED;
                    escrow.Payment = new PaymentInfo
                    {
                        Provider = "Mock",
                        PaymentIntentId = intentId,
                        AuthorizedAt = DateTime.UtcNow
                    };
                    escrow.AddEvent(EscrowEventType.AUTHORIZED, "Payment authorized", context.BuyerId);
                    await _escrowRepo.UpdateAsync(escrow);
                }

                _logger.LogInformation("Authorized payment {IntentId} for escrow {EscrowId}", intentId, context.EscrowId);

                return SagaStepResult.Successful(new Dictionary<string, object>
                {
                    { "paymentIntentId", intentId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to authorize payment");
                return SagaStepResult.Failed($"Payment authorization failed: {ex.Message}");
            }
        }

        public async Task<bool> CompensateAsync(SagaContext context)
        {
            try
            {
                var escrow = await _escrowRepo.GetByIdAsync(context.EscrowId);
                if (escrow?.Payment?.PaymentIntentId != null)
                {
                    // Cancel/void the authorization
                    await _paymentProvider.CancelAsync(escrow.Payment.PaymentIntentId);

                    // Record cancellation
                    var payment = new Payment
                    {
                        EscrowId = context.EscrowId,
                        Provider = "Mock",
                        IntentId = escrow.Payment.PaymentIntentId,
                        Action = PaymentAction.CANCEL,
                        Amount = context.Amount,
                        Status = PaymentStatus.SUCCEEDED
                    };
                    await _paymentRepo.CreateAsync(payment);

                    _logger.LogInformation("Compensated: Cancelled payment authorization {IntentId}", 
                        escrow.Payment.PaymentIntentId);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compensate AuthorizePayment");
                return false;
            }
        }
    }
}

