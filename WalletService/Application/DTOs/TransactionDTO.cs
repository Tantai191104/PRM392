using System;

namespace WalletService.Application.DTOs
{
    public class TransactionDTO
    {
        public string? Id { get; set; }
        public string? WalletId { get; set; }
        public decimal Amount { get; set; }
        public string? Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Description { get; set; }
    }
}