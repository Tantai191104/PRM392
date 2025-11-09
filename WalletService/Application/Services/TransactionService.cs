using System;
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

        public async Task<List<Transaction>> GetAllTransactionsAsync()
        {
            return await _transRepo.GetAllAsync();
        }

        public async Task<List<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _transRepo.GetByDateRangeAsync(startDate, endDate);
        }
    }
}