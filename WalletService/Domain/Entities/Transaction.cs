using System;

namespace WalletService.Domain.Entities
{
    public class Transaction
    {
    public string? Id { get; set; }
    public string? WalletId { get; set; }
        public decimal Amount { get; set; }
    public string? Type { get; set; } // Deposit, Withdraw, Transfer
        public DateTime CreatedAt { get; set; }
        public string? Description { get; set; }
    }
}