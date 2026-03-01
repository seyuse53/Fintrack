using System;

namespace FinTrack.Core.Models
{
    public enum InvestmentTransactionType
    {
        Buy,
        Sell
    }

    public class InvestmentTransaction
    {
        public int Id { get; set; }
        
        // Link to the Asset
        public int InvestmentAssetId { get; set; }
        public InvestmentAsset? InvestmentAsset { get; set; }

        public InvestmentTransactionType Type { get; set; }
        
        public decimal Amount { get; set; }       // Quantity bought/sold
        public decimal UnitPrice { get; set; }    // Price per unit at the time
        public decimal Fee { get; set; }          // Commission or transaction fee
        public decimal TotalCost { get; set; }    // Total money spent/received (Amount * UnitPrice + Fee, or - Fee)
        
        public DateTime Date { get; set; } = DateTime.Now;
        public string? Notes { get; set; }

        // Optional link to a BankAccount from which money was drawn/deposited
        public int? LinkedBankAccountId { get; set; }
        public BankAccount? LinkedBankAccount { get; set; }

        // Optional link to a CreditCardAccount from which money was drawn/deposited
        public int? LinkedCreditCardAccountId { get; set; }
        public CreditCardAccount? LinkedCreditCardAccount { get; set; }
    }
}
