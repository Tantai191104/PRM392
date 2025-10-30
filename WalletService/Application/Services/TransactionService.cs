using System.Collections.Generic;
using System.Threading.Tasks;
using WalletService.Domain.Entities;
using WalletService.Infrastructure.Repositories;

namespace WalletService.Application.Services
{
    public class TransactionService
    {
        private readonly TransactionRepository _transRepo;
        public TransactionService(TransactionRepository transRepo)
        {
            _transRepo = transRepo;
        }
        public async Task<List<Transaction>> GetTransactionsByWalletIdAsync(string walletId)
        {
            return await _transRepo.GetByWalletIdAsync(walletId);
        }
        public async Task CreateTransactionAsync(Transaction transaction)
        {
            await _transRepo.CreateAsync(transaction);
        }
    }
}