using EscrowService.Domain.Entities;
using EscrowService.Infrastructure.Providers;
using EscrowService.Infrastructure.Repositories;

namespace EscrowService.Application.Saga.Steps
{
    public class CapturePaymentStep : ISagaStep
    {
        private readonly IPaymentProvider _paymentProvider;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IEscrowRepository _escrowRepo;
        private readonly ILogger<CapturePaymentStep> _logger;

        public string StepName => "CapturePayment";

        public CapturePaymentStep(
            IPaymentProvider paymentProvider,
            IPaymentRepository paymentRepo,
            IEscrowRepository escrowRepo,
            ILogger<CapturePaymentStep> logger)
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
                var escrow = await _escrowRepo.GetByIdAsync(context.EscrowId);
                if (escrow?.Payment?.PaymentIntentId == null)
                {
                    return SagaStepResult.Failed("No payment intent found");
                }

                // Capture the authorized payment
                await _paymentProvider.CaptureAsync(escrow.Payment.PaymentIntentId);

                // Record capture
                var payment = new Payment
                {
                    EscrowId = context.EscrowId,
                    Provider = "Mock",
                    IntentId = escrow.Payment.PaymentIntentId,
                    Action = PaymentAction.CAPTURE,
                    Amount = context.Amount,
                    Status = PaymentStatus.SUCCEEDED
                };
                await _paymentRepo.CreateAsync(payment);

                // Update escrow
                escrow.Status = EscrowStatus.HOLDING;
                escrow.Payment.CapturedAt = DateTime.UtcNow;
                escrow.AddEvent(EscrowEventType.CAPTURED, "Payment captured and held in escrow", null);
                await _escrowRepo.UpdateAsync(escrow);

                _logger.LogInformation("Captured payment for escrow {EscrowId}", context.EscrowId);

                return SagaStepResult.Successful();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture payment");
                return SagaStepResult.Failed($"Payment capture failed: {ex.Message}");
            }
        }

        public async Task<bool> CompensateAsync(SagaContext context)
        {
            try
            {
                var escrow = await _escrowRepo.GetByIdAsync(context.EscrowId);
                if (escrow?.Payment?.PaymentIntentId != null)
                {
                    // Refund the captured payment
                    await _paymentProvider.RefundAsync(escrow.Payment.PaymentIntentId, context.Amount);

                    // Record refund
                    var payment = new Payment
                    {
                        EscrowId = context.EscrowId,
                        Provider = "Mock",
                        IntentId = escrow.Payment.PaymentIntentId,
                        Action = PaymentAction.REFUND,
                        Amount = context.Amount,
                        Status = PaymentStatus.SUCCEEDED
                    };
                    await _paymentRepo.CreateAsync(payment);

                    escrow.Status = EscrowStatus.REFUNDED;
                    escrow.Payment.RefundedAt = DateTime.UtcNow;
                    await _escrowRepo.UpdateAsync(escrow);

                    _logger.LogInformation("Compensated: Refunded payment for escrow {EscrowId}", context.EscrowId);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compensate CapturePayment");
                return false;
            }
        }
    }
}

