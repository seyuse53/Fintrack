using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FinTrack.Core.Models;

namespace FinTrack.Data.Services
{
    /// <summary>
    /// Fetches and caches monthly TÜFE (CPI) rates from TÜİK's public API.
    /// Falls back to a static table when the API is unavailable.
    /// </summary>
    public class InflationService
    {
        // Empty constructor for fallback
        public InflationService() { }

        // TÜİK open-data endpoint — no API key required.
        // Series TP.FE.OKTG01 = monthly TÜFE annual change rate.
        private const string TuikApiBase =
            "https://data.tuik.gov.tr/api/data?code=TP.FE.OKTG01&startDate={0}-{1:D2}&endDate={0}-{1:D2}&lang=TR";

        // ── Static fallback table (annual TÜFE % for each month) ─────────────
        private static readonly Dictionary<(int Year, int Month), double> FallbackTable = new()
        {
            // 2023
            { (2023, 1),  57.68 }, { (2023, 2),  55.18 }, { (2023, 3),  50.51 },
            { (2023, 4),  43.68 }, { (2023, 5),  39.59 }, { (2023, 6),  38.21 },
            { (2023, 7),  47.83 }, { (2023, 8),  58.94 }, { (2023, 9),  61.53 },
            { (2023, 10), 61.36 }, { (2023, 11), 62.00 }, { (2023, 12), 64.77 },
            // 2024
            { (2024, 1),  64.86 }, { (2024, 2),  67.07 }, { (2024, 3),  68.50 },
            { (2024, 4),  69.80 }, { (2024, 5),  75.45 }, { (2024, 6),  71.60 },
            { (2024, 7),  61.78 }, { (2024, 8),  51.97 }, { (2024, 9),  49.38 },
            { (2024, 10), 48.58 }, { (2024, 11), 47.09 }, { (2024, 12), 44.38 },
            // 2025
            { (2025, 1),  42.12 }, { (2025, 2),  39.05 }, { (2025, 3),  38.10 },
            { (2025, 4),  37.86 }, { (2025, 5),  35.05 }, { (2025, 6),  35.19 },
            { (2025, 7),  33.68 }, { (2025, 8),  31.72 }, { (2025, 9),  30.98 },
            { (2025, 10), 30.36 }, { (2025, 11), 29.65 }, { (2025, 12), 30.89 },
            // 2026
            { (2026, 1),  30.65 }, { (2026, 2),  29.50 }, { (2026, 3),  28.80 },
            { (2026, 4),  28.20 }, { (2026, 5),  27.50 }, { (2026, 6),  27.10 }
        };

        public InflationService(AppDbContext db)
        {
            // We don't store db to avoid ObjectDisposedException on view switch.
            // Using short-lived contexts instead.
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the annual CPI rate for the given month/year.
        /// If the requested month is not yet published, falls back up to 12 months
        /// to find the most recent available data.
        /// Order per attempt: DB cache → TÜİK API → static fallback table.
        /// </summary>
        public async Task<double?> GetCpiRateAsync(int year, int month)
        {
            var result = await GetCpiRateWithPeriodAsync(year, month);
            return result?.Rate;
        }

        /// <summary>
        /// Like GetCpiRateAsync but also returns which year/month the data actually
        /// came from (may differ when falling back to a previous month).
        /// </summary>
        public async Task<(double Rate, int ActualYear, int ActualMonth)?> GetCpiRateWithPeriodAsync(int year, int month)
        {
            int tryYear = year;
            int tryMonth = month;

            for (int attempt = 0; attempt < 12; attempt++)
            {
                double? rate = await TryGetSingleMonthAsync(tryYear, tryMonth);
                if (rate.HasValue)
                    return (rate.Value, tryYear, tryMonth);

                // Step back one month
                tryMonth--;
                if (tryMonth < 1) { tryMonth = 12; tryYear--; }
            }

            return null;
        }

        private static bool _isApiDown = false;

        private async Task<double?> TryGetSingleMonthAsync(int year, int month)
        {
            // 1. DB cache
            try
            {
                using var db = AppDbContext.CreateNew();
                if (db != null)
                {
                    var cached = db.InflationCaches
                                    .FirstOrDefault(i => i.Year == year && i.Month == month);
                    if (cached != null)
                        return cached.CpiRate;
                }
            }
            catch { }

            // 2. TÜİK API
            if (!_isApiDown)
            {
                double? apiRate = await FetchFromTuikAsync(year, month);
                if (apiRate == -1)
                {
                    _isApiDown = true;
                }
                else if (apiRate.HasValue)
                {
                    UpsertCache(year, month, apiRate.Value);
                    return apiRate;
                }
            }

            // 3. Static fallback table
            if (FallbackTable.TryGetValue((year, month), out double fallback))
                return fallback;

            return null;
        }

        /// <summary>Synchronous wrapper — avoid on UI thread; prefer async version.</summary>
        public double? GetCpiRate(int year, int month)
        {
            return GetCpiRateAsync(year, month).GetAwaiter().GetResult();
        }

        /// <summary>Forces a fresh fetch from TÜİK and updates cache. Returns new rate or null.</summary>
        public async Task<double?> RefreshFromTuikAsync(int year, int month)
        {
            _isApiDown = false; // Reset the flag so manual refresh can try again
            double? rate = await FetchFromTuikAsync(year, month);
            if (rate == -1) rate = null; // Normalize back to null for UI
            if (rate.HasValue)
                UpsertCache(year, month, rate.Value);
            return rate;
        }

        /// <summary>Returns the last cached entry's fetch timestamp, or null.</summary>
        public DateTime? LastFetchedAt()
        {
            try
            {
                using var db = AppDbContext.CreateNew();
                if (db == null) return null;
                
                return db.InflationCaches
                          .OrderByDescending(i => i.FetchedAt)
                          .Select(i => (DateTime?)i.FetchedAt)
                          .FirstOrDefault();
            }
            catch { return null; }
        }


        // ── Internals ─────────────────────────────────────────────────────────

        private static async Task<double?> FetchFromTuikAsync(int year, int month)
        {
            try
            {
                string url = string.Format(TuikApiBase, year, month);
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(3); // Reduced timeout to prevent UI freezes
                string json = await client.GetStringAsync(url);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                JsonElement data = root;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d))
                    data = d;

                if (data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        foreach (var prop in item.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Number
                                && prop.Value.TryGetDouble(out double v) && v > 0)
                                return v;

                            if (prop.Value.ValueKind == JsonValueKind.String
                                && double.TryParse(
                                    prop.Value.GetString()?.Replace(',', '.'),
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out double sv) && sv > 0)
                                return sv;
                        }
                    }
                }
            }
            catch
            {
                // Network error / timeout / parse failure — return -1 to signal API down
                return -1;
            }
            return null;
        }

        private void UpsertCache(int year, int month, double rate)
        {
            try
            {
                using var db = AppDbContext.CreateNew();
                if (db == null) return;

                var existing = db.InflationCaches
                                  .FirstOrDefault(i => i.Year == year && i.Month == month);
                if (existing == null)
                {
                    db.InflationCaches.Add(new InflationCache
                    {
                        Year = year, Month = month, CpiRate = rate, FetchedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.CpiRate = rate;
                    existing.FetchedAt = DateTime.UtcNow;
                }
                db.SaveChanges();
            }
            catch { }
        }
    }
}
