using System;
using System.Collections.Concurrent;
using System.Linq;
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
        /// Veritabanındaki LastKnownPrice değerlerini bellek cache'ine yükler.
        /// Uygulama açılışında çağrılmalıdır.
        /// </summary>
        public static void LoadPricesFromAssets(System.Collections.Generic.IEnumerable<InvestmentAsset> assets)
        {
            foreach (var asset in assets)
            {
                if (asset.LastKnownPrice > 0 && !string.IsNullOrWhiteSpace(asset.Symbol))
                {
                    _cachedPrices[asset.Symbol.ToUpperInvariant()] = asset.LastKnownPrice;
                }
            }
        }

        /// <summary>
        /// Bellek cache'indeki tüm fiyatları veritabanındaki varlıklara yazar.
        /// Fiyat güncellendikten sonra veya uygulama kapanırken çağrılmalıdır.
        /// </summary>
        public static void SavePricesToAssets(System.Collections.Generic.IEnumerable<InvestmentAsset> assets)
        {
            var now = DateTime.Now;
            foreach (var asset in assets)
            {
                if (!string.IsNullOrWhiteSpace(asset.Symbol))
                {
                    var upperSymbol = asset.Symbol.ToUpperInvariant();
                    if (_cachedPrices.TryGetValue(upperSymbol, out var price))
                    {
                        asset.LastKnownPrice = price;
                        asset.LastPriceUpdate = now;
                    }
                }
            }
        }

        /// <summary>
        /// Bellek cache'indeki fiyatları PriceHistory tablosuna yazar.
        /// Günde 1 kayıt per sembol: aynı gün tekrar yazılırsa Close güncellenir, Low/High ayarlanır.
        /// </summary>
        public static void RecordPriceHistory(
            System.Collections.Generic.IEnumerable<InvestmentAsset> assets,
            System.Collections.Generic.IList<PriceHistory> existingHistories,
            System.Action<PriceHistory> addNewRecord,
            string source = "Manuel")
        {
            var today = DateTime.Today;

            foreach (var asset in assets)
            {
                if (string.IsNullOrWhiteSpace(asset.Symbol)) continue;
                var upperSymbol = asset.Symbol.ToUpperInvariant();
                
                if (!_cachedPrices.TryGetValue(upperSymbol, out var currentPrice)) continue;
                if (currentPrice <= 0) continue;

                // Bugün bu sembol için kayıt var mı?
                var todayRecord = existingHistories
                    .FirstOrDefault(h => h.Symbol == upperSymbol && h.Date == today);

                if (todayRecord != null)
                {
                    // Mevcut kaydı güncelle
                    todayRecord.ClosePrice = currentPrice;
                    todayRecord.LowPrice = Math.Min(todayRecord.LowPrice, currentPrice);
                    todayRecord.HighPrice = Math.Max(todayRecord.HighPrice, currentPrice);
                    todayRecord.Source = source;
                }
                else
                {
                    // Yeni kayıt oluştur
                    var newRecord = new PriceHistory
                    {
                        Symbol = upperSymbol,
                        ClosePrice = currentPrice,
                        LowPrice = currentPrice,
                        HighPrice = currentPrice,
                        Date = today,
                        Source = source
                    };
                    addNewRecord(newRecord);
                    existingHistories.Add(newRecord);
                }
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

        /// <summary>
        /// GenelPara API'den döviz veya altın fiyatını çeker.
        /// Döviz: https://api.genelpara.com/json/?list=doviz&sembol=USD
        /// Altın: https://api.genelpara.com/json/?list=altin&sembol=GA (Gram Altın)
        /// </summary>
        public static async Task<bool> FetchFromGenelParaAsync(string symbol, string listType = "doviz")
        {
            if (string.IsNullOrWhiteSpace(symbol)) return false;

            try
            {
                string gpSymbol = MapSymbolToGenelPara(symbol, listType);
                string url = $"https://api.genelpara.com/json/?list={listType}&sembol={gpSymbol}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return false;

                string json = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // GenelPara response: { "success": true, "data": { "SYMBOL": { "satis": "45.25", ... } } }
                if (root.TryGetProperty("success", out var successNode) && successNode.GetBoolean())
                {
                    if (root.TryGetProperty("data", out var dataNode))
                    {
                        foreach (var prop in dataNode.EnumerateObject())
                        {
                            if (prop.Value.TryGetProperty("satis", out var satisNode))
                            {
                                string satisStr = satisNode.GetString() ?? "";
                                satisStr = satisStr.Replace(".", "").Replace(",", ".");
                                if (decimal.TryParse(satisStr, System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture, out decimal price) && price > 0)
                                {
                                    SetPrice(symbol, price);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GenelPara error for {symbol}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// GenelPara sembol eşleştirmesi
        /// </summary>
        private static string MapSymbolToGenelPara(string symbol, string listType)
        {
            symbol = symbol.ToUpperInvariant().Trim();

            if (listType == "altin")
            {
                return symbol switch
                {
                    "XAU" => "GA",      // Gram Altın
                    "CAU" => "C",       // Çeyrek Altın
                    "YAU" => "Y",       // Yarım Altın
                    "TAU" => "T",       // Tam Altın
                    _ => symbol
                };
            }

            // Döviz: GenelPara zaten USD, EUR, GBP kullanıyor
            return symbol;
        }

        /// <summary>
        /// Kategori bazlı akıllı rotalama: varlık kategorisine göre doğru API'yi seçer.
        /// Ayarlardan okunur, yazılımdan değişiklik gerektirmez.
        /// </summary>
        public static async Task<bool> FetchPriceSmartAsync(string symbol, string? category)
        {
            var provider = SettingsManager.GetProviderForCategory(category);

            return provider switch
            {
                ApiProviderType.YahooFinance => await FetchRealTimePriceAsync(symbol),
                ApiProviderType.GenelPara => category switch
                {
                    "Altın" => await FetchFromGenelParaAsync(symbol, "altin"),
                    "Kripto Para" => await FetchFromGenelParaAsync(symbol, "kripto"),
                    _ => await FetchFromGenelParaAsync(symbol, "doviz")
                },
                ApiProviderType.Manual => false, // Manuel modda API çekmiyoruz
                _ => await FetchRealTimePriceAsync(symbol) // Fallback: Yahoo
            };
        }
    }
}
