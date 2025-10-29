using EscrowService.Infrastructure.ExternalServices;

namespace EscrowService.Application.Saga.Steps
{
    public class ReserveListingStep : ISagaStep
    {
        private readonly IProductServiceClient _productClient;
        private readonly ILogger<ReserveListingStep> _logger;

        public string StepName => "ReserveListing";

        public ReserveListingStep(IProductServiceClient productClient, ILogger<ReserveListingStep> logger)
        {
            _productClient = productClient;
            _logger = logger;
        }

        public async Task<SagaStepResult> ExecuteAsync(SagaContext context)
        {
            try
            {
                // Call ProductService to set listing status to InTransaction
                var success = await _productClient.UpdateListingStatusAsync(context.ListingId, "InTransaction");

                if (!success)
                {
                    return SagaStepResult.Failed("Failed to reserve listing");
                }

                _logger.LogInformation("Reserved listing {ListingId}", context.ListingId);

                return SagaStepResult.Successful();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reserve listing");
                return SagaStepResult.Failed($"Listing reservation failed: {ex.Message}");
            }
        }

        public async Task<bool> CompensateAsync(SagaContext context)
        {
            try
            {
                // Unreserve listing - set back to Published
                await _productClient.UpdateListingStatusAsync(context.ListingId, "Published");
                _logger.LogInformation("Compensated: Unreserved listing {ListingId}", context.ListingId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compensate ReserveListing");
                return false;
            }
        }
    }
}

