using AiService.Models;
using AiService.Infrastructure;
using AiService.Application.Services;
using AiService.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiService.Services
{
    /// <summary>
    /// Battery prediction service using RAG (Retrieval-Augmented Generation)
    /// with FREE Hugging Face embeddings (sentence-transformers/all-MiniLM-L6-v2)
    /// </summary>
    public class BatteryPredictionService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorRepository _vectorRepo;
        private readonly PredictionRepository _predRepo;
        private readonly ILogger<BatteryPredictionService> _logger;

        public BatteryPredictionService(
            IEmbeddingService embeddingService,
            IVectorRepository vectorRepo,
            PredictionRepository predRepo,
            ILogger<BatteryPredictionService> logger)
        {
            _embeddingService = embeddingService;
            _vectorRepo = vectorRepo;
            _predRepo = predRepo;
            _logger = logger;
        }

        /// <summary>
        /// Predict battery status and price using RAG
        /// </summary>
        public async Task<PriceSuggestionResult> PredictAsync(BatteryFeaturesDto input, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Starting RAG prediction for battery: {ProductId}", input.ProductId);

                var batteryText = ConvertToEmbeddingText(input);
                _logger.LogInformation("Battery text: {Text}", batteryText);

                var embedding = await _embeddingService.GenerateEmbeddingAsync(batteryText);
                _logger.LogInformation("Generated embedding with {Dimensions} dimensions", embedding.Length);

                var similarBatteries = await _vectorRepo.SearchSimilarBatteriesAsync(embedding, k: 5);
                _logger.LogInformation("Found {Count} similar batteries", similarBatteries.Count);

                string predictedStatus;
                decimal suggestedPrice;
                List<SimilarBatteryInfo> similarBatteryInfos;

                if (similarBatteries.Any())
                {
                    predictedStatus = similarBatteries
                        .GroupBy(x => x.Battery.ActualStatus)
                        .OrderByDescending(g => g.Count())
                        .First().Key;

                    var totalWeight = similarBatteries.Sum(x => x.SimilarityScore);
                    suggestedPrice = (decimal)(similarBatteries
                        .Sum(x => (double)x.Battery.ActualPrice * x.SimilarityScore) / totalWeight);

                    similarBatteryInfos = similarBatteries.Select(x => new SimilarBatteryInfo
                    {
                        ProductId = x.Battery.ProductId,
                        Brand = x.Battery.Brand,
                        Name = x.Battery.Name,
                        Price = (float)x.Battery.ActualPrice,
                        Status = x.Battery.ActualStatus,
                        SimilarityScore = x.SimilarityScore
                    }).ToList();
                }
                else
                {
                    predictedStatus = EstimateStatusFromFeatures(input);
                    suggestedPrice = EstimatePriceFromFeatures(input);
                    similarBatteryInfos = new List<SimilarBatteryInfo>();

                    _logger.LogWarning("No similar batteries found. Using fallback estimation: Status={Status}, Price={Price:C2}",
                        predictedStatus, suggestedPrice);
                }

                var result = new PriceSuggestionResult
                {
                    Status = predictedStatus,
                    SuggestedPrice = (double)suggestedPrice,
                    SimilarBatteries = similarBatteryInfos
                };

                // Save prediction history (commented out until PredictionRepository is updated)
                // await _predRepo.SavePredictionAsync(result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during RAG prediction");
                throw;
            }
        }

        /// <summary>
        /// Convert battery features to text representation
        /// </summary>
        private string ConvertToEmbeddingText(BatteryFeaturesDto input)
        {
            return $"Brand: {input.Brand}, Condition: {input.Condition}, " +
                   $"Capacity: {input.CapacityMah}mAh, Voltage: {input.VoltageNumeric}V, " +
                   $"Cycles: {input.CycleCount}, Age: {input.AgeMonths} months, " +
                   $"Physical Score: {input.PhysicalConditionScore}, " +
                   $"Remaining: {input.RemainingCapacityPercent}%";
        }

        /// <summary>
        /// Rule-based status estimation (fallback)
        /// </summary>
        private string EstimateStatusFromFeatures(BatteryFeaturesDto input)
        {
            if (input.RemainingCapacityPercent >= 80) return "Excellent";
            if (input.RemainingCapacityPercent >= 60) return "Good";
            if (input.RemainingCapacityPercent >= 40) return "Fair";
            return "Poor";
        }

        /// <summary>
        /// Rule-based price estimation (fallback)
        /// </summary>
        private decimal EstimatePriceFromFeatures(BatteryFeaturesDto input)
        {
            decimal basePrice = (decimal)input.CapacityMah * 0.01m; // $ per mAh
            decimal conditionMultiplier = (decimal)input.RemainingCapacityPercent / 100m;
            decimal brandMultiplier = input.Brand.ToLower() switch
            {
                "tesla" => 1.5m,
                "lg" => 1.3m,
                "samsung" => 1.3m,
                "panasonic" => 1.2m,
                "byd" => 1.1m,
                _ => 1.0m
            };
            return Math.Round(basePrice * conditionMultiplier * brandMultiplier, 2);
        }

        /// <summary>
        /// Get all battery vectors
        /// </summary>
        public async Task<List<BatteryVector>> GetAllBatteryVectorsAsync()
        {
            return await _vectorRepo.GetAllVectorsAsync();
        }

        /// <summary>
        /// Get all predictions (for debugging) - Commented out until repository updated
        /// </summary>
        public Task<List<PriceSuggestionResult>> GetAllPredictionsAsync()
        {
            // TODO: Update PredictionRepository to support RAG predictions
            _logger.LogWarning("GetAllPredictionsAsync not yet implemented for RAG");
            return Task.FromResult(new List<PriceSuggestionResult>());
        }

        /// <summary>
        /// Save battery vector for continuous learning after user confirms actual price
        /// </summary>
        public async Task AddTrainingDataWithActualPriceAsync(
            BatteryFeaturesDto features,
            string actualStatus,
            decimal actualPrice,
            CancellationToken ct = default)
        {
            try
            {
                // Validate price range
                if (actualPrice < 50 || actualPrice > 5000)
                {
                    _logger.LogWarning("Price out of valid range: ${Price}. Skipping training data save.", actualPrice);
                    return;
                }

                _logger.LogInformation("Saving battery vector: ProductId={ProductId}, Status={Status}, Price={Price:C2}",
                    features.ProductId, actualStatus, actualPrice);

                // Generate embedding for the new battery
                var batteryText = ConvertToEmbeddingText(features);
                var embedding = await _embeddingService.GenerateEmbeddingAsync(batteryText);

                // Create battery vector document
                var vector = new BatteryVector
                {
                    ProductId = features.ProductId,
                    Name = features.Name,
                    Brand = features.Brand,
                    Condition = features.Condition,
                    CapacityAh = (float)features.CapacityMah / 1000f,  // Convert mAh to Ah
                    VoltageNumeric = features.VoltageNumeric,
                    CycleCount = features.CycleCount,
                    AgeMonths = features.AgeMonths,
                    PhysicalConditionScore = features.PhysicalConditionScore,
                    RemainingCapacityPercent = features.RemainingCapacityPercent,
                    ActualStatus = actualStatus,
                    ActualPrice = (float)actualPrice,
                    Embedding = embedding,
                    CreatedAt = DateTime.UtcNow
                };

                // Save to MongoDB
                await _vectorRepo.InsertBatteryVectorAsync(vector);
                _logger.LogInformation("Battery vector saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving battery vector");
            }
        }
    }
}
