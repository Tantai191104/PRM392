using System.Collections.Generic;
using System.Threading.Tasks;
using WalletService.Domain.Entities;
using MongoDB.Driver;

namespace WalletService.Infrastructure.Repositories
{
    public class WalletRepository
    {
        private readonly IMongoCollection<Wallet> _wallets;
        public WalletRepository(IMongoDatabase database)
        {
            _wallets = database.GetCollection<Wallet>("Wallets");
        }
        public async Task<Wallet?> GetByUserIdAsync(string userId)
        {
            return await _wallets.Find(w => w.UserId == userId).FirstOrDefaultAsync();
        }
        public async Task CreateAsync(Wallet wallet)
        {
            await _wallets.InsertOneAsync(wallet);
        }
        public async Task UpdateAsync(Wallet wallet)
        {
            await _wallets.ReplaceOneAsync(w => w.Id == wallet.Id, wallet);
        }

        public async Task<List<Wallet>> GetAllAsync()
        {
            return await _wallets.Find(_ => true).ToListAsync();
        }
    }
}