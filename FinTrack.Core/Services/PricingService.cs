using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FinTrack.Core.Models;
using System.Diagnostics;

namespace FinTrack.Core.Services
{
    public static class PricingService
    {
        // Thread-safe dictionary to keep the "live" prices in-memory
        private static readonly ConcurrentDictionary<string, decimal> _cachedPrices = new();
        private static readonly HttpClient _httpClient = new HttpClient();

        static PricingService()
        {
            // Optional: User-Agent is sometimes required by public APIs
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FinTrack/1.0");
        }

        public static decimal GetCurrentPrice(string symbol, decimal fallbackCost = 0m)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return fallbackCost;
            
            var upperSymbol = symbol.ToUpperInvariant();
            if (_cachedPrices.TryGetValue(upperSymbol, out var currentPrice))
            {
                return currentPrice;
            }
            
            return fallbackCost;
        }

        public static void SetPrice(string symbol, decimal price)
        {
            if (!string.IsNullOrWhiteSpace(symbol))
            {
                _cachedPrices[symbol.ToUpperInvariant()] = price;
            }
        }

        /// <summary>
        /// Maps common local symbols to Yahoo Finance symbols.
        /// </summary>
        private static string MapSymbolToYahoo(string symbol)
        {
            symbol = symbol.ToUpperInvariant().Trim();
            
            // Example mappings:
            switch (symbol)
            {
                case "USD": return "USDTRY=X";
                case "EUR": return "EURTRY=X";
                case "GBP": return "GBPTRY=X";
                case "XAU": return "XAU=X"; // Note: This gets Ounce price in USD, special logic needed for Gram TRY
                case "BTC": return "BTC-USD"; // Note: Special logic needed for TRY conversion if wanted
            }

            // By default, if it doesn't contain a dot or equal sign, assume it's a BIST stock
            if (!symbol.Contains(".") && !symbol.Contains("=") && !symbol.Contains("-"))
            {
                return $"{symbol}.IS"; // e.g., THYAO -> THYAO.IS
            }

            return symbol;
        }

        /// <summary>
        /// Fetches the real-time price from Yahoo Finance API.
        /// XAU is automatically converted to Gram Gold in TRY.
        /// BTC is fetched in USD, then converted to TRY.
        /// </summary>
        public static async Task<bool> FetchRealTimePriceAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return false;

            try
            {
                string yahooSymbol = MapSymbolToYahoo(symbol);
                string url = $"https://query1.finance.yahoo.com/v8/finance/chart/{yahooSymbol}?interval=1d&range=1d";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return false;

                string json = await response.Content.ReadAsStringAsync();
                
                // Parse the JSON. Yahoo's v8 chart API structure:
                // chart -> result[0] -> meta -> regularMarketPrice
                using JsonDocument doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("chart", out var chartNode) && 
                    chartNode.TryGetProperty("result", out var resultNode) && 
                    resultNode.GetArrayLength() > 0)
                {
                    var metaNode = resultNode[0].GetProperty("meta");
                    if (metaNode.TryGetProperty("regularMarketPrice", out var priceNode))
                    {
                        decimal rawPrice = priceNode.GetDecimal();

                        // Special Cases:
                        if (symbol.Equals("XAU", StringComparison.OrdinalIgnoreCase))
                        {
                            // 1 Ounce = 31.1034768 grams. 
                            // Raw price is Ounce in USD. Need USDTRY to calculate Gram Gold in TRY.
                            await FetchRealTimePriceAsync("USD"); // Ensure we have latest USD
                            if (_cachedPrices.TryGetValue("USD", out var usdPrice))
                            {
                                decimal gramTRY = (rawPrice * usdPrice) / 31.1034768m;
                                SetPrice("XAU", gramTRY);
                                return true;
                            }
                            return false; // Failed to calculate Gram Gold without USD
                        }
                        
                        if (symbol.Equals("BTC", StringComparison.OrdinalIgnoreCase))
                        {
                            // Raw price is BTC in USD.
                            await FetchRealTimePriceAsync("USD");
                            if (_cachedPrices.TryGetValue("USD", out var usdPrice))
                            {
                                decimal btcTRY = rawPrice * usdPrice;
                                SetPrice("BTC", btcTRY);
                                return true;
                            }
                            return false;
                        }

                        // Normal assignment
                        SetPrice(symbol, rawPrice);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching price for {symbol}: {ex.Message}");
            }

            return false;
        }
    }
}
