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

        public decimal LastKnownPrice { get; set; }   // Son bilinen güncel fiyat (uygulama kapansa bile kalır)
        public DateTime? LastPriceUpdate { get; set; } // Fiyatın en son güncellenme zamanı

        public decimal? CustomCurrentValue { get; set; }     // BES gibi varlıklar için manuel girilen güncel değer
        public decimal? CustomStateContribution { get; set; } // BES gibi varlıklar için manuel girilen devlet katkısı

        public DateTime? BesStartDate { get; set; }        // BES Sözleşme Yürürlük Tarihi
        public DateTime? BesRetirementDate { get; set; }   // BES Emeklilik Tarihi
        public string? BesContractNo { get; set; }         // BES Müşteri/Sözleşme No
        public DateTime? ParticipantBirthDate { get; set; } // BES Doğum Tarihi
        public int? BesRetirementAge { get; set; }          // BES Emeklilik Yaşı

        public List<InvestmentTransaction> Transactions { get; set; } = new();
    }
}
