using EscrowService.Domain.Entities;
using EscrowService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EscrowService.Web.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhookController : ControllerBase
    {
        private readonly IWebhookRepository _webhookRepo;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(IWebhookRepository webhookRepo, ILogger<WebhookController> logger)
        {
            _webhookRepo = webhookRepo;
            _logger = logger;
        }

        /// <summary>
        /// Receive webhook from payment gateway
        /// </summary>
        [HttpPost("payment")]
        public async Task<IActionResult> PaymentWebhook([FromBody] Dictionary<string, object> payload)
        {
            try
            {
                var eventId = payload.GetValueOrDefault("event_id")?.ToString() ?? Guid.NewGuid().ToString();
                var eventType = payload.GetValueOrDefault("event_type")?.ToString() ?? "unknown";

                // Check for duplicate
                var existing = await _webhookRepo.GetByEventIdAsync("payment_gateway", eventId);
                if (existing != null)
                {
                    _logger.LogInformation("Duplicate webhook event {EventId}, skipping", eventId);
                    return Ok(new { received = true, duplicate = true });
                }

                // Store webhook
                var webhook = new Webhook
                {
                    Source = "payment_gateway",
                    EventId = eventId,
                    EventType = eventType,
                    Payload = payload
                };

                await _webhookRepo.CreateAsync(webhook);

                _logger.LogInformation("Received webhook event {EventId} of type {EventType}", eventId, eventType);

                // Process webhook asynchronously (in background job in production)
                // For MVP, just log it
                _logger.LogInformation("Webhook {EventId} stored for processing", eventId);

                return Ok(new { received = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}

