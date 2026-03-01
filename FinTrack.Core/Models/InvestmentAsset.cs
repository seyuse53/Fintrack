using System;
using System.Collections.Generic;

namespace FinTrack.Core.Models
{
    public class InvestmentAsset
    {
        public int Id { get; set; }
        public required string Name { get; set; }    // e.g. "Gram Altın", "Amerikan Doları", "THYAO"
        public required string Symbol { get; set; }  // e.g. "XAU", "USD", "THYAO"
        public string? Category { get; set; }        // e.g. "Altın", "Döviz", "Hisse"
        
        public decimal TotalAmount { get; set; }     // Total quantity owned (e.g. 15.5 grams)
        public decimal AverageCost { get; set; }     // Average price paid per unit

        public List<InvestmentTransaction> Transactions { get; set; } = new();
    }
}
