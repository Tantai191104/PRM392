namespace EscrowService.Application.Saga
{
    /// <summary>
    /// Interface for a single step in a Saga
    /// </summary>
    public interface ISagaStep
    {
        string StepName { get; }
        Task<SagaStepResult> ExecuteAsync(SagaContext context);
        Task<bool> CompensateAsync(SagaContext context);
    }

    public class SagaStepResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();

        public static SagaStepResult Successful(Dictionary<string, object>? data = null)
        {
            return new SagaStepResult { Success = true, Data = data ?? new() };
        }

        public static SagaStepResult Failed(string errorMessage)
        {
            return new SagaStepResult { Success = false, ErrorMessage = errorMessage };
        }
    }

    public class SagaContext
    {
    public string EscrowId { get; set; } = string.Empty;
    public string BuyerId { get; set; } = string.Empty;
    public string SellerId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public decimal Amount { get; set; }
    public Dictionary<string, object> SharedData { get; set; } = new();
    public List<string> CompletedSteps { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    }
}

