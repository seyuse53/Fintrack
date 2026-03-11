using System;

namespace FinTrack.Core.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string? Description { get; set; }
        public string? GroupId { get; set; }
        
        // Foreign Key → Category
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // Optional FK → CreditCardAccount (null = cash payment)
        public int? CreditCardAccountId { get; set; }
        public CreditCardAccount? CreditCardAccount { get; set; }

        // Optional FK → BankAccount (null = cash payment if Credit Card is also null)
        public int? BankAccountId { get; set; }
        public BankAccount? BankAccount { get; set; }
    }
}
