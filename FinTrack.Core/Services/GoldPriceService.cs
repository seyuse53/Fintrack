using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinTrack.Core.Services
{
    public static class GoldPriceService
    {
        public static async Task<decimal?> GetGramGoldPriceAsync(DateTime date)
        {
            try
            {
                decimal? gcPrice = await FetchYahooPrice("GC=F", date);
                decimal? tryPrice = await FetchYahooPrice("TRY=X", date);

                if (gcPrice.HasValue && tryPrice.HasValue)
                {
                    return (gcPrice.Value / 31.1034768m) * tryPrice.Value;
                }
            }
            catch
            {
                // Silently ignore failures
            }
            return null;
        }

        private static async Task<decimal?> FetchYahooPrice(string symbol, DateTime targetDate)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?interval=1d&range=5y";
            var response = await client.GetStringAsync(url);
            
            var doc = JsonDocument.Parse(response);
            var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
            var timestamps = result.GetProperty("timestamp");
            var closePrices = result.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close");

            decimal? closestPrice = null;
            DateTime bestDate = DateTime.MinValue;

            for (int i = 0; i < timestamps.GetArrayLength(); i++)
            {
                if (closePrices[i].ValueKind == JsonValueKind.Number)
                {
                    var ts = timestamps[i].GetInt64();
                    var price = closePrices[i].GetDecimal();
                    var dt = DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime.Date;
                    
                    if (dt <= targetDate.Date)
                    {
                        if (dt > bestDate)
                        {
                            bestDate = dt;
                            closestPrice = price;
                        }
                    }
                }
            }
            return closestPrice;
        }
    }
}
