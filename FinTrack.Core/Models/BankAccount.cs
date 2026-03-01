using System;
using System.Collections.Generic;

namespace FinTrack.Core.Models
{
    public class BankAccount
    {
        public int Id { get; set; }

        /// <summary>Name of the bank, e.g. "Ziraat Bankası", "Garanti BBVA"</summary>
        public required string BankName { get; set; }

        /// <summary>Name or type of the account, e.g. "Maaş Hesabı", "Vadesiz TL"</summary>
        public required string AccountName { get; set; }

        /// <summary>Optional IBAN for reference</summary>
        public string? IBAN { get; set; }

        /// <summary>Initial balance when the account is added to the system</summary>
        public decimal InitialBalance { get; set; } = 0;

        /// <summary>When false, the account is hidden from the payment method picker</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property for transactions associated with this bank account
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
