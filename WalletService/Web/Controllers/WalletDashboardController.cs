using WalletService.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WalletService.Web.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class WalletDashboardController : ControllerBase
    {
        private readonly WalletAppService _walletService;
        private readonly TransactionService _transactionService;
        private readonly ILogger<WalletDashboardController> _logger;

        public WalletDashboardController(
            WalletAppService walletService,
            TransactionService transactionService,
            ILogger<WalletDashboardController> logger)
        {
            _walletService = walletService;
            _transactionService = transactionService;
            _logger = logger;
        }

        /// <summary>
        /// 💰 Get wallet statistics for dashboard
        /// </summary>
        [HttpGet("wallets")]
        public async Task<IActionResult> GetWalletStats()
        {
            try
            {
                // Get all wallets
                var allWallets = await _walletService.GetAllWalletsAsync();
                
                // Get all transactions
                var allTransactions = await _transactionService.GetAllTransactionsAsync();

                // Calculate time ranges
                var now = DateTime.UtcNow;
                var todayStart = now.Date;
                
                // Today's transactions
                var todayTransactions = allTransactions
                    .Where(t => t.CreatedAt >= todayStart)
                    .ToList();

                // Calculate deposits and withdrawals
                var deposits = allTransactions
                    .Where(t => t.Type == "DEPOSIT" || t.Type == "Deposit")
                    .Sum(t => t.Amount);
                
                var withdrawals = allTransactions
                    .Where(t => t.Type == "WITHDRAW" || t.Type == "Withdrawal")
                    .Sum(t => t.Amount);

                // Transaction trends (last 7 days)
                var last7Days = Enumerable.Range(0, 7)
                    .Select(i => now.Date.AddDays(-i))
                    .Reverse()
                    .ToList();

                var transactionTrends = last7Days.Select(date =>
                {
                    var dayTransactions = allTransactions
                        .Where(t => t.CreatedAt.Date == date)
                        .ToList();

                    return new
                    {
                        Date = date.ToString("yyyy-MM-dd"),
                        Count = dayTransactions.Count,
                        Amount = dayTransactions.Sum(t => t.Amount)
                    };
                }).ToList();

                var stats = new
                {
                    TotalWallets = allWallets.Count,
                    TotalBalance = allWallets.Sum(w => w.Balance),
                    TotalTransactions = allTransactions.Count,
                    TotalDeposits = deposits,
                    TotalWithdrawals = withdrawals,
                    TodayTransactions = todayTransactions.Count,
                    TransactionTrends = transactionTrends
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wallet stats");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// 📊 Get transaction statistics by date range
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactionStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(start, end);

                var stats = new
                {
                    TotalCount = transactions.Count,
                    TotalAmount = transactions.Sum(t => t.Amount),
                    ByType = transactions
                        .GroupBy(t => t.Type)
                        .Select(g => new
                        {
                            Type = g.Key,
                            Count = g.Count(),
                            Amount = g.Sum(t => t.Amount)
                        })
                        .ToList(),
                    ByDay = transactions
                        .GroupBy(t => t.CreatedAt.Date)
                        .Select(g => new
                        {
                            Date = g.Key.ToString("yyyy-MM-dd"),
                            Count = g.Count(),
                            Amount = g.Sum(t => t.Amount)
                        })
                        .OrderBy(x => x.Date)
                        .ToList()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction stats");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// 🔝 Get top wallets by balance
        /// </summary>
        [HttpGet("top-wallets")]
        public async Task<IActionResult> GetTopWallets([FromQuery] int limit = 10)
        {
            try
            {
                var wallets = await _walletService.GetAllWalletsAsync();
                
                var topWallets = wallets
                    .OrderByDescending(w => w.Balance)
                    .Take(limit)
                    .Select(w => new
                    {
                        w.Id,
                        w.UserId,
                        w.Balance
                    })
                    .ToList();

                return Ok(topWallets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top wallets");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}
