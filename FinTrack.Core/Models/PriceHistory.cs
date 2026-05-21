using System;

namespace FinTrack.Core.Models
{
    /// <summary>
    /// Günlük fiyat geçmişi kaydı.
    /// Her sembol için günde 1 kayıt tutulur.
    /// Aynı gün tekrar güncellenirse: Close güncellenir, Low/High ayarlanır.
    /// </summary>
    public class PriceHistory
    {
        public int Id { get; set; }

        public required string Symbol { get; set; }   // Varlık sembolü (XAU, USD, THYAO...)

        public decimal ClosePrice { get; set; }        // Gün sonu (son güncellenen) fiyat
        public decimal LowPrice { get; set; }          // Gün içi en düşük fiyat
        public decimal HighPrice { get; set; }         // Gün içi en yüksek fiyat

        public DateTime Date { get; set; }             // Sadece tarih (günlük kayıt)

        public string Source { get; set; } = "Manuel"; // "Yahoo" veya "Manuel"
    }
}
