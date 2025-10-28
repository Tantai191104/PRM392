using AiService.Models;

namespace AiService.Infrastructure.Repositories
{
    public interface IVectorRepository
    {
        Task InsertBatteryVectorAsync(BatteryVector batteryVector);
        Task<List<BatteryVectorSimilarity>> SearchSimilarBatteriesAsync(float[] queryEmbedding, int k = 5);
        Task<List<BatteryVector>> GetAllVectorsAsync();
    }

    public class BatteryVectorSimilarity
    {
        public BatteryVector Battery { get; set; } = null!;
        public float SimilarityScore { get; set; }
    }
}
