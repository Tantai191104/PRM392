using MediaService.Domain.Entities;
using MongoDB.Driver;

namespace MediaService.Infrastructure.Repositories
{
    public interface IMediaRepository
    {
        Task<Media> CreateAsync(Media media);
        Task<Media?> GetByIdAsync(string id);
        Task<List<Media>> GetByListingIdAsync(string listingId);
        Task<bool> DeleteAsync(string id);
        Task<List<Media>> GetAllAsync();
    }

    public class MediaRepository : IMediaRepository
    {
        private readonly IMongoCollection<Media> _collection;

        public MediaRepository(IMongoClient client, string databaseName)
        {
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<Media>("listing_media");

            // Create indexes
            var indexKeys = Builders<Media>.IndexKeys.Ascending(m => m.ListingId);
            var indexModel = new CreateIndexModel<Media>(indexKeys);
            _collection.Indexes.CreateOneAsync(indexModel);
        }

        public async Task<Media> CreateAsync(Media media)
        {
            media.CreatedAt = DateTime.UtcNow;
            await _collection.InsertOneAsync(media);
            return media;
        }

        public async Task<Media?> GetByIdAsync(string id)
        {
            return await _collection.Find(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<Media>> GetByListingIdAsync(string listingId)
        {
            return await _collection.Find(m => m.ListingId == listingId)
                .SortBy(m => m.Order)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _collection.DeleteOneAsync(m => m.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<List<Media>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }
    }
}

