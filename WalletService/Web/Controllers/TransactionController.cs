using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WalletService.Application.DTOs;
using WalletService.Application.Services;
using WalletService.Domain.Entities;

namespace WalletService.Web.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionController : ControllerBase
    {
        private readonly TransactionService _transactionService;
        public TransactionController(TransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>
        /// Get all transactions for a wallet
        /// </summary>
        [HttpGet("wallets/{walletId}")]
        [ProducesResponseType(typeof(Transaction[]), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactionsByWalletId(string walletId)
        {
            var transactions = await _transactionService.GetTransactionsByWalletIdAsync(walletId);
            return Ok(transactions);
        }

        /// <summary>
        /// Create a new transaction
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Transaction), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTransaction([FromBody] TransactionDTO transactionDto)
        {
            if (transactionDto == null || string.IsNullOrEmpty(transactionDto.WalletId))
            {
                return BadRequest(new { message = "Invalid transaction data" });
            }

            var transaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                WalletId = transactionDto.WalletId,
                Amount = transactionDto.Amount,
                Type = transactionDto.Type ?? "Unknown",
                CreatedAt = DateTime.UtcNow,
                Description = transactionDto.Description
            };

            await _transactionService.CreateTransactionAsync(transaction);
            return CreatedAtAction(nameof(GetTransactionsByWalletId), new { walletId = transaction.WalletId }, transaction);
        }
    }
}