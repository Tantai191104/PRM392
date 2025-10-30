using EscrowService.Domain.Entities;
using EscrowService.Infrastructure.Repositories;

namespace EscrowService.Application.Saga.Steps
{
    public class CreateEscrowStep : ISagaStep
    {
        private readonly IEscrowRepository _escrowRepo;
        private readonly ILogger<CreateEscrowStep> _logger;

        public string StepName => "CreateEscrow";

        public CreateEscrowStep(IEscrowRepository escrowRepo, ILogger<CreateEscrowStep> logger)
        {
            _escrowRepo = escrowRepo;
            _logger = logger;
        }

        public async Task<SagaStepResult> ExecuteAsync(SagaContext context)
        {
            try
            {
                var escrow = new Escrow
                {
                    ProductId = context.ProductId,
                    BuyerId = context.BuyerId,
                    SellerId = context.SellerId,
                    AmountTotal = context.Amount,
                    AmountHold = context.Amount,
                    Status = EscrowStatus.CREATED,
                    Terms = new EscrowTerms
                    {
                        AutoReleaseAt = DateTime.UtcNow.AddDays(7),
                        ReleaseConditions = new List<string> { "Buyer confirmation", "Delivery confirmation" }
                    }
                };

                escrow.AddEvent(EscrowEventType.CREATED, "Escrow created", context.BuyerId);

                await _escrowRepo.CreateAsync(escrow);

                context.EscrowId = escrow.Id;
                _logger.LogInformation("Created escrow {EscrowId} for product {ProductId}", escrow.Id, context.ProductId);

                return SagaStepResult.Successful(new Dictionary<string, object>
                {
                    { "escrowId", escrow.Id }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create escrow");
                return SagaStepResult.Failed($"Failed to create escrow: {ex.Message}");
            }
        }

        public async Task<bool> CompensateAsync(SagaContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(context.EscrowId))
                    return true;

                var escrow = await _escrowRepo.GetByIdAsync(context.EscrowId);
                if (escrow != null)
                {
                    escrow.Status = EscrowStatus.FAILED;
                    escrow.AddEvent(EscrowEventType.FAILED, "Saga failed - escrow cancelled", null);
                    await _escrowRepo.UpdateAsync(escrow);
                    _logger.LogInformation("Compensated: Marked escrow {EscrowId} as FAILED", context.EscrowId);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compensate CreateEscrow");
                return false;
            }
        }
    }
}

