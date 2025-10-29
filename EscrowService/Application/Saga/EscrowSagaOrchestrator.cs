using EscrowService.Application.Saga.Steps;

namespace EscrowService.Application.Saga
{
    /// <summary>
    /// Orchestrator for Escrow Saga Pattern
    /// Coordinates distributed transaction across multiple services
    /// </summary>
    public class EscrowSagaOrchestrator
    {
        private readonly ILogger<EscrowSagaOrchestrator> _logger;
        private readonly IServiceProvider _serviceProvider;

        public EscrowSagaOrchestrator(
            ILogger<EscrowSagaOrchestrator> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<SagaExecutionResult> ExecuteCreateEscrowSagaAsync(SagaContext context)
        {
            var result = new SagaExecutionResult { Success = true };
            var executedSteps = new Stack<ISagaStep>();

            // Define saga steps in order
            var steps = new List<ISagaStep>
            {
                ActivatorUtilities.CreateInstance<CreateEscrowStep>(_serviceProvider),
                ActivatorUtilities.CreateInstance<AuthorizePaymentStep>(_serviceProvider),
                ActivatorUtilities.CreateInstance<ReserveListingStep>(_serviceProvider),
                ActivatorUtilities.CreateInstance<CreateOrderStep>(_serviceProvider),
                ActivatorUtilities.CreateInstance<CapturePaymentStep>(_serviceProvider)
            };

            _logger.LogInformation("Starting Escrow Saga for Listing {ListingId}, Buyer {BuyerId}", 
                context.ListingId, context.BuyerId);

            try
            {
                // Execute forward steps
                foreach (var step in steps)
                {
                    _logger.LogInformation("Executing saga step: {StepName}", step.StepName);
                    
                    var stepResult = await step.ExecuteAsync(context);
                    
                    if (stepResult.Success)
                    {
                        executedSteps.Push(step);
                        context.CompletedSteps.Add(step.StepName);
                        
                        // Merge step data into shared context
                        foreach (var kvp in stepResult.Data)
                        {
                            context.SharedData[kvp.Key] = kvp.Value;
                        }
                        
                        _logger.LogInformation("Step {StepName} completed successfully", step.StepName);
                    }
                    else
                    {
                        // Step failed - trigger compensating transactions
                        _logger.LogWarning("Step {StepName} failed: {Error}", step.StepName, stepResult.ErrorMessage);
                        context.Errors.Add($"{step.StepName}: {stepResult.ErrorMessage}");
                        result.Success = false;
                        result.ErrorMessage = stepResult.ErrorMessage;
                        break;
                    }
                }

                // If any step failed, compensate
                if (!result.Success)
                {
                    _logger.LogWarning("Saga failed, starting compensation for {Count} steps", executedSteps.Count);
                    await CompensateAsync(executedSteps, context);
                    result.CompensationCompleted = true;
                }
                else
                {
                    _logger.LogInformation("Escrow Saga completed successfully for {EscrowId}", context.EscrowId);
                    result.EscrowId = context.EscrowId;
                    result.OrderId = context.OrderId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in Saga execution");
                result.Success = false;
                result.ErrorMessage = $"Saga execution failed: {ex.Message}";
                
                // Compensate on exception
                await CompensateAsync(executedSteps, context);
                result.CompensationCompleted = true;
            }

            return result;
        }

        private async Task CompensateAsync(Stack<ISagaStep> executedSteps, SagaContext context)
        {
            _logger.LogInformation("Starting compensation transactions");

            while (executedSteps.Count > 0)
            {
                var step = executedSteps.Pop();
                try
                {
                    _logger.LogInformation("Compensating step: {StepName}", step.StepName);
                    var compensated = await step.CompensateAsync(context);
                    
                    if (compensated)
                    {
                        _logger.LogInformation("Step {StepName} compensated successfully", step.StepName);
                    }
                    else
                    {
                        _logger.LogWarning("Step {StepName} compensation failed (non-critical)", step.StepName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error compensating step {StepName}", step.StepName);
                    // Continue compensating other steps even if one fails
                }
            }

            _logger.LogInformation("Compensation completed");
        }
    }

    public class SagaExecutionResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? EscrowId { get; set; }
        public string? OrderId { get; set; }
        public bool CompensationCompleted { get; set; }
    }
}

