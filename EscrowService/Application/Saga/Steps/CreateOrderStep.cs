using EscrowService.Infrastructure.ExternalServices;
using EscrowService.Infrastructure.Repositories;

namespace EscrowService.Application.Saga.Steps
{
    public class CreateOrderStep : ISagaStep
    {
    private readonly IOrderServiceClient _orderClient;
    private readonly IEscrowRepository _escrowRepo;
    private readonly ILogger<CreateOrderStep> _logger;

        public string StepName => "CreateOrder";

        public CreateOrderStep(IOrderServiceClient orderClient, IEscrowRepository escrowRepo, ILogger<CreateOrderStep> logger)
        {
            _orderClient = orderClient;
            _escrowRepo = escrowRepo;
            _logger = logger;
        }

        public async Task<SagaStepResult> ExecuteAsync(SagaContext context)
        {
            try
            {
                // Call OrderService to create order
                var orderId = await _orderClient.CreateOrderAsync(context.BuyerId, context.ProductId, context.EscrowId);

                if (string.IsNullOrEmpty(orderId))
                {
                    return SagaStepResult.Failed("Failed to create order");
                }

                context.OrderId = orderId;
                _logger.LogInformation("Created order {OrderId} for escrow {EscrowId}", orderId, context.EscrowId);

                // Cập nhật OrderId vào escrow
                var escrow = await _escrowRepo.GetByIdAsync(context.EscrowId);
                if (escrow != null)
                {
                    escrow.OrderId = orderId;
                    await _escrowRepo.UpdateAsync(escrow);
                }

                return SagaStepResult.Successful(new Dictionary<string, object>
                {
                    { "orderId", orderId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order");
                return SagaStepResult.Failed($"Order creation failed: {ex.Message}");
            }
        }

        public async Task<bool> CompensateAsync(SagaContext context)
        {
            try
            {
                if (!string.IsNullOrEmpty(context.OrderId))
                {
                    // Cancel the order
                    await _orderClient.CancelOrderAsync(context.OrderId);
                    _logger.LogInformation("Compensated: Cancelled order {OrderId}", context.OrderId);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compensate CreateOrder");
                return false;
            }
        }
    }
}

