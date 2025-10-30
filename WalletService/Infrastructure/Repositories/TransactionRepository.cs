using System.Collections.Generic;
using System.Threading.Tasks;
using WalletService.Domain.Entities;
using MongoDB.Driver;

namespace WalletService.Infrastructure.Repositories
{
    public class TransactionRepository
    {
        private readonly IMongoCollection<Transaction> _transactions;
        public TransactionRepository(IMongoDatabase database)
        {
            _transactions = database.GetCollection<Transaction>("Transactions");
        }
        public async Task<List<Transaction>> GetByWalletIdAsync(string walletId)
        {
            return await _transactions.Find(t => t.WalletId == walletId).ToListAsync();
        }
        public async Task CreateAsync(Transaction transaction)
        {
            await _transactions.InsertOneAsync(transaction);
        }
    }
}