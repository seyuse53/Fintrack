using System;

namespace FinTrack.Core.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public decimal DisplayAmount => Math.Abs(Amount);
        public DateTime Date { get; set; } = DateTime.Now;
        public string? Description { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string FormattedAmount
        {
            get
            {
                string sign = "";
                if (Category?.Type == TransactionType.Income) sign = "+";
                else if (Category?.Type == TransactionType.Expense) sign = "-";
                else if (Category?.Type == TransactionType.Transfer) sign = Amount > 0 ? "+" : "-";
                return $"{sign}₺{DisplayAmount:N2}";
            }
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string ForegroundColor
        {
            get
            {
                if (Category?.Type == TransactionType.Income) return "#27AE60";
                if (Category?.Type == TransactionType.Expense) return "#C62828";
                if (Category?.Type == TransactionType.Transfer) return Amount > 0 ? "#27AE60" : "#C62828";
                return "#333333";
            }
        }
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
