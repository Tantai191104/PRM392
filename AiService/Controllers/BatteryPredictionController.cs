using Microsoft.AspNetCore.Mvc;
using AiService.Services;
using AiService.Models;

namespace AiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BatteryPredictionController : ControllerBase
    {
        private readonly BatteryPredictionService _predictionService;
        private readonly ILogger<BatteryPredictionController> _logger;

        public BatteryPredictionController(
            BatteryPredictionService predictionService,
            ILogger<BatteryPredictionController> logger)
        {
            _predictionService = predictionService;
            _logger = logger;
        }

        /// <summary>
        /// Predict battery status and price using RAG (Retrieval-Augmented Generation)
        /// Returns suggestions based on similar batteries in vector database
        /// </summary>
        [HttpPost("predict")]
        public async Task<IActionResult> Predict([FromBody] BatteryFeaturesDto data, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Received prediction request for battery: {ProductId}", data.ProductId);
                var result = await _predictionService.PredictAsync(data, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting battery");
                return StatusCode(500, new { message = "Lỗi khi dự đoán", error = ex.Message });
            }
        }

        /// <summary>
        /// Add training data with actual user price after product creation
        /// This builds the vector database for future similarity searches
        /// </summary>
        [HttpPost("add-training")]
        public async Task<IActionResult> AddTraining([FromBody] AddTrainingRequest request, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ActualStatus) || request.ActualPrice <= 0)
                {
                    return BadRequest(new { message = "Cần cung cấp ActualStatus và ActualPrice hợp lệ." });
                }

                _logger.LogInformation(
                    "Adding training data for ProductId: {ProductId}, Price: ${Price}",
                    request.BatteryFeatures.ProductId,
                    request.ActualPrice);

                await _predictionService.AddTrainingDataWithActualPriceAsync(
                    request.BatteryFeatures, 
                    request.ActualStatus, 
                    (decimal)request.ActualPrice, 
                    ct
                );

                return Ok(new { 
                    message = "Đã lưu battery vector vào database.",
                    actualPrice = request.ActualPrice,
                    actualStatus = request.ActualStatus
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding training data");
                return StatusCode(500, new { message = "Lỗi khi lưu training data", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all battery vectors (for debugging/testing)
        /// </summary>
        [HttpGet("battery-vectors")]
        public async Task<IActionResult> GetBatteryVectors()
        {
            try
            {
                var vectors = await _predictionService.GetAllBatteryVectorsAsync();
                return Ok(new
                {
                    count = vectors.Count,
                    batteries = vectors.Select(v => new
                    {
                        v.Id,
                        v.ProductId,
                        v.Brand,
                        v.Name,
                        v.ActualPrice,
                        v.ActualStatus,
                        v.CreatedAt,
                        embeddingDimensions = v.Embedding.Length
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting battery vectors");
                return StatusCode(500, new { message = "Lỗi khi lấy battery vectors", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all predictions (for debugging/testing)
        /// </summary>
        [HttpGet("predictions")]
        public async Task<IActionResult> GetPredictions()
        {
            try
            {
                var predictions = await _predictionService.GetAllPredictionsAsync();
                return Ok(new
                {
                    count = predictions.Count,
                    predictions = predictions.Select(p => new
                    {
                        p.Status,
                        p.SuggestedPrice,
                        SimilarBatteriesCount = p.SimilarBatteries.Count,
                        SimilarBatteries = p.SimilarBatteries.Select(sb => new 
                        {
                            sb.ProductId,
                            sb.Brand,
                            sb.Name,
                            sb.Price,
                            sb.Status,
                            sb.SimilarityScore
                        })
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting predictions");
                return StatusCode(500, new { message = "Lỗi khi lấy predictions", error = ex.Message });
            }
        }
    }
}
