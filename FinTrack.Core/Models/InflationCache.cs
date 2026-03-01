using System;

namespace FinTrack.Core.Models
{
    /// <summary>
    /// Caches monthly CPI (TÜFE) rates fetched from TÜİK to avoid repeated API calls.
    /// </summary>
    public class InflationCache
    {
        public int Id { get; set; }

        /// <summary>The year this CPI rate belongs to.</summary>
        public int Year { get; set; }

        /// <summary>The month (1–12) this CPI rate belongs to.</summary>
        public int Month { get; set; }

        /// <summary>Annual CPI change rate in percent (e.g. 38.3 means 38.3%).</summary>
        public double CpiRate { get; set; }

        /// <summary>When this record was fetched from TÜİK.</summary>
        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
    }
}
