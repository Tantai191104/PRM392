namespace EscrowService.Infrastructure.Providers
{
    /// <summary>
    /// Mock payment provider for MVP
    /// In production, replace with real payment gateway (VNPay, MoMo, Stripe, etc.)
    /// </summary>
    public class MockPaymentProvider : IPaymentProvider
    {
        private readonly ILogger<MockPaymentProvider> _logger;

        public MockPaymentProvider(ILogger<MockPaymentProvider> logger)
        {
            _logger = logger;
        }

        public Task<string> AuthorizeAsync(string customerId, decimal amount)
        {
            var intentId = $"pi_mock_{Guid.NewGuid():N}";
            _logger.LogInformation("Mock: Authorized {Amount} VND for customer {CustomerId}, Intent: {IntentId}", 
                amount, customerId, intentId);
            
            // Simulate payment gateway delay
            Thread.Sleep(100);
            
            return Task.FromResult(intentId);
        }

        public Task CaptureAsync(string paymentIntentId)
        {
            _logger.LogInformation("Mock: Captured payment {IntentId}", paymentIntentId);
            Thread.Sleep(100);
            return Task.CompletedTask;
        }

        public Task RefundAsync(string paymentIntentId, decimal amount)
        {
            _logger.LogInformation("Mock: Refunded {Amount} VND for payment {IntentId}", amount, paymentIntentId);
            Thread.Sleep(100);
            return Task.CompletedTask;
        }

        public Task CancelAsync(string paymentIntentId)
        {
            _logger.LogInformation("Mock: Cancelled payment authorization {IntentId}", paymentIntentId);
            Thread.Sleep(100);
            return Task.CompletedTask;
        }
    }
}

