using WalletService.Domain.Entities;
using WalletService.Infrastructure.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WalletService.Application.Services
{
    public class WalletAppService
    {
        private readonly WalletRepository _walletRepo;
        public WalletAppService(WalletRepository walletRepo)
        {
            _walletRepo = walletRepo;
        }

        public async Task<bool> TransferAsync(string fromUserId, string toUserId, decimal amount)
        {
            var fromWallet = await _walletRepo.GetByUserIdAsync(fromUserId);
            var toWallet = await _walletRepo.GetByUserIdAsync(toUserId);
            if (fromWallet == null || toWallet == null || fromWallet.Balance < amount)
                return false;
            fromWallet.Balance -= amount;
            toWallet.Balance += amount;
            await _walletRepo.UpdateAsync(fromWallet);
            await _walletRepo.UpdateAsync(toWallet);
            return true;
        }

        public async Task<bool> ReleaseAsync(string userId, decimal amount)
        {
            var wallet = await _walletRepo.GetByUserIdAsync(userId);
            if (wallet == null)
                return false;
            wallet.Balance += amount;
            await _walletRepo.UpdateAsync(wallet);
            return true;
        }

        public async Task<bool> HoldAsync(string userId, decimal amount)
        {
            var wallet = await _walletRepo.GetByUserIdAsync(userId);
            if (wallet == null || wallet.Balance < amount)
                return false;
            wallet.Balance -= amount;
            await _walletRepo.UpdateAsync(wallet);
            return true;
        }

        public async Task<Wallet?> GetWalletByUserIdAsync(string userId)
        {
            return await _walletRepo.GetByUserIdAsync(userId);
        }

        public async Task CreateWalletAsync(Wallet wallet)
        {
            await _walletRepo.CreateAsync(wallet);
        }

        public async Task UpdateWalletAsync(Wallet wallet)
        {
            await _walletRepo.UpdateAsync(wallet);
        }

        public async Task<List<Wallet>> GetAllWalletsAsync()
        {
            return await _walletRepo.GetAllAsync();
        }
    }
}
