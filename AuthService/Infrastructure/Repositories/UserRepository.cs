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

        public async Task<IEnumerable<User>> GetAllAsync() =>
            await _collection.Find(_ => true).ToListAsync();
    }
}
