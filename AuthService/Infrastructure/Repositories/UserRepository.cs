using AuthService.Domain.Entities;
using MongoDB.Driver;

namespace AuthService.Infrastructure.Repositories
{
    public class UserRepository
    {
        private readonly IMongoCollection<User> _collection;

        public UserRepository(IMongoClient client, string databaseName)
        {
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<User>("Users");
        }

        public async Task CreateAsync(User user) => await _collection.InsertOneAsync(user);

        public async Task<User?> GetByEmailAsync(string email) =>
            await _collection.Find(u => u.Email == email).FirstOrDefaultAsync();

        public async Task<User?> GetByRefreshTokenAsync(string refreshToken) =>
            await _collection.Find(u => u.RefreshToken == refreshToken).FirstOrDefaultAsync();

        public async Task<User?> GetByIdAsync(string id) =>
            await _collection.Find(u => u.Id == id).FirstOrDefaultAsync();

        public async Task UpdateAsync(User user) =>
            await _collection.ReplaceOneAsync(u => u.Id == user.Id, user);

        public async Task<User?> UpdateProfileAsync(string id, AuthService.Application.DTOs.UpdateProfileDto dto)
        {
            var update = Builders<User>.Update;
            var updates = new List<UpdateDefinition<User>>();

            if (dto.FullName != null) updates.Add(update.Set(u => u.FullName, dto.FullName));
            if (dto.DisplayName != null) updates.Add(update.Set(u => u.DisplayName, dto.DisplayName));
            if (dto.Phone != null) updates.Add(update.Set(u => u.Phone, dto.Phone));
            if (dto.AvatarUrl != null) updates.Add(update.Set(u => u.AvatarUrl, dto.AvatarUrl));
            if (dto.Bio != null) updates.Add(update.Set(u => u.Bio, dto.Bio));

            if (dto.Address != null)
            {
                updates.Add(update.Set(u => u.Address, dto.Address));
            }

            if (updates.Count == 0) return await GetByIdAsync(id);

            updates.Add(update.Set(u => u.UpdatedAt, DateTime.UtcNow));

            var combined = update.Combine(updates);

            var options = new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After };

            return await _collection.FindOneAndUpdateAsync(u => u.Id == id, combined, options);
        }

        public async Task<IEnumerable<User>> GetAllAsync() =>
            await _collection.Find(_ => true).ToListAsync();
    }
}
