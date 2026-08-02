using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FinTrack.Core.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;
using FinTrack.Core.Helpers;

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
                AppLogger.Error($"Error fetching price for {symbol}: {ex.Message}");
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
                AppLogger.Error($"GenelPara error for {symbol}: {ex.Message}");
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
                ApiProviderType.WebScraper => category switch
                {
                    "Döviz" => await FetchDovizFromDovizComScraperAsync(symbol),
                    "Altın" => await FetchAltinFromDovizComScraperAsync(symbol),
                    "Hisse Senedi" => await FetchFromDovizComScraperAsync(symbol),
                    "Hisse" => await FetchFromDovizComScraperAsync(symbol),
                    "Kripto Para" => await FetchKriptoFromDovizComScraperAsync(symbol),
                    _ => false
                },
                ApiProviderType.Manual => false, // Manuel modda API çekmiyoruz
                _ => await FetchRealTimePriceAsync(symbol) // Fallback: Yahoo
            };
        }

        /// <summary>
        /// Hisse senetleri için Yahoo Finance yerine doğrudan borsa.doviz.com kazıyıcısı kullanır.
        /// API limitlerine takılmaz ve doğrudan HTML içinden regex ile okur.
        /// </summary>
        public static async Task<bool> FetchFromDovizComScraperAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return false;
            symbol = symbol.ToUpperInvariant().Trim();

            try
            {
                using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://borsa.doviz.com/hisseler");
                // Anti-bot korumasını aşmak için sıradan bir tarayıcı gibi davranıyoruz
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
                request.Headers.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
                
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return false;

                string html = await response.Content.ReadAsStringAsync();

                // Hedef: <tr id="TTRAK" ...> ... <td class="text-bold">441,25</td>
                string pattern = $@"<tr\s+id=""{symbol}"".*?>\s*<td.*?>.*?</td>\s*<td\s+class=""text-bold"">\s*([\d\.,]+)\s*</td>";
                var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                var match = regex.Match(html);
                if (match.Success)
                {
                    string priceStr = match.Groups[1].Value.Trim().Replace(".", "").Replace(",", ".");
                    if (decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price) && price > 0)
                    {
                        SetPrice(symbol, price);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Borsa scraper error for {symbol}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Kripto paralar için Binance API üzerinden USDT fiyatını alır ve USD/TRY ile TL'ye çevirir.
        /// </summary>
        public static async Task<bool> FetchKriptoFromBinanceAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return false;
            symbol = symbol.ToUpperInvariant().Trim();

            try
            {
                string binanceSymbol = $"{symbol}USDT";
                string url = $"https://api.binance.com/api/v3/ticker/price?symbol={binanceSymbol}";
                
                using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0");
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("price", out var priceElement))
                    {
                        string priceStr = priceElement.GetString() ?? "0";
                        if (decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal priceUsd) && priceUsd > 0)
                        {
                            // USD/TRY kuru ile TL'ye çevir
                            await FetchRealTimePriceAsync("USD");
                            if (_cachedPrices.TryGetValue("USD", out var usdPrice))
                            {
                                decimal tryPrice = priceUsd * usdPrice;
                                SetPrice(symbol, tryPrice);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Binance error for {symbol}: {ex.Message}");
            }
            
            return false;
        }

        /// <summary>
        /// Kripto paralar için doğrudan doviz.com kazıyıcısı kullanır.
        /// </summary>
        public static async Task<bool> FetchKriptoFromDovizComScraperAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return false;
            symbol = symbol.ToUpperInvariant().Trim();

            try
            {
                using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://www.doviz.com/kripto-paralar");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                request.Headers.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
                
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return false;

                string html = await response.Content.ReadAsStringAsync();

                // Hedef: <div>BTC</div> ... <td class="text-bold">$64.046</td> <td>₺2.960.785</td>
                string pattern = $@"<div>\s*{symbol}\s*</div>.*?<td.*?>.*?</td>\s*<td.*?>.*?([\d\.,]+)\s*</td>";
                var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                var match = regex.Match(html);
                if (match.Success)
                {
                    string priceStr = match.Groups[1].Value.Trim().Replace(".", "").Replace(",", ".");
                    if (decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price) && price > 0)
                    {
                        SetPrice(symbol, price);
                        return true;
                    }
                }

                // Ana tabloda bulamazsa, alt sayfasına (slug) istek at:
                string slug = symbol.ToLowerInvariant();
                if (slug == "s") slug = "sonic"; // Özel eşleştirme: S sembolü için
                
                using var subRequest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"https://www.doviz.com/kripto-paralar/{slug}");
                subRequest.Headers.Add("User-Agent", "Mozilla/5.0");
                var subResponse = await _httpClient.SendAsync(subRequest);
                
                if (subResponse.IsSuccessStatusCode)
                {
                    string subHtml = await subResponse.Content.ReadAsStringAsync();
                    // Hedef yapı: class="text-xs text-blue-gray-2">SONIC/TRY</div> ... <div class="text-md font-semibold text-white mt-4">₺1,39</div>
                    string subPattern = @"/TRY.*?</div>\s*<div.*?>.*?([\d\.,]+)</div>";
                    var subRegex = new System.Text.RegularExpressions.Regex(subPattern, System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    var subMatch = subRegex.Match(subHtml);
                    if (subMatch.Success)
                    {
                        string priceStr = subMatch.Groups[1].Value.Trim().Replace(".", "").Replace(",", ".");
                        if (decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price) && price > 0)
                        {
                            SetPrice(symbol, price);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Crypto scraper error for {symbol}: {ex.Message}");
            }

            // Kazıma başarısız olursa Binance API'ye (Global Kripto) Fallback yap
            bool binanceSuccess = await FetchKriptoFromBinanceAsync(symbol);
            if (binanceSuccess) return true;

            // Binance da başarısız olursa orijinal Yahoo yöntemine Fallback
            return await FetchRealTimePriceAsync(symbol);
        }

        /// <summary>
        /// Altın için doğrudan altin.doviz.com kazıyıcısı kullanır.
        /// HTML içinden data-socket-key özniteliği aracılığıyla arar.
        /// </summary>
        public static async Task<bool> FetchAltinFromDovizComScraperAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return false;
            symbol = symbol.ToUpperInvariant().Trim();

            // Sizin kullandığınız Altın sembollerini doviz.com socket key'lerine eşleştirelim
            string dataKey = symbol switch
            {
                "XAU" => "gram-altin",
                "CAU" => "ceyrek-altin",
                "YAU" => "yarim-altin",
                "TAU" => "tam-altin",
                _ => symbol.ToLowerInvariant() + "-altin"
            };

            try
            {
                using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://altin.doviz.com/");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
                request.Headers.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
                
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return false;

                string html = await response.Content.ReadAsStringAsync();

                // Hedef: data-socket-key="ceyrek-altin" data-socket-attr="ask"...>10.325,09</td>
                string pattern = $@"data-socket-key=""{dataKey}""\s+data-socket-attr=""ask"".*?>\s*([\d\.,]+)\s*</td>";
                var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                var match = regex.Match(html);
                if (match.Success)
                {
                    string priceStr = match.Groups[1].Value.Trim().Replace(".", "").Replace(",", ".");
                    if (decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price) && price > 0)
                    {
                        SetPrice(symbol, price);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Altin Scraping error for {symbol}: {ex.Message}");
            }

            // Kazıma başarısız olursa GenelPara Altın yöntemine Fallback
            return await FetchFromGenelParaAsync(symbol, "altin");
        }

        /// <summary>
        /// Döviz için doğrudan kur.doviz.com kazıyıcısı kullanır.
        /// HTML içinden data-socket-key özniteliği aracılığıyla arar.
        /// </summary>
        public static async Task<bool> FetchDovizFromDovizComScraperAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return false;
            symbol = symbol.ToUpperInvariant().Trim();

            // Döviz için data-socket-key doğrudan sembolün kendisidir (örn. USD, EUR)
            string dataKey = symbol;

            try
            {
                using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://kur.doviz.com/");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
                request.Headers.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
                
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return false;

                string html = await response.Content.ReadAsStringAsync();

                // Hedef: data-socket-key="USD" data-socket-attr="ask"...>46,2874</td>
                string pattern = $@"data-socket-key=""{dataKey}""\s+data-socket-attr=""ask"".*?>\s*([\d\.,]+)\s*</td>";
                var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                var match = regex.Match(html);
                if (match.Success)
                {
                    string priceStr = match.Groups[1].Value.Trim().Replace(".", "").Replace(",", ".");
                    if (decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price) && price > 0)
                    {
                        SetPrice(symbol, price);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Doviz Scraping error for {symbol}: {ex.Message}");
            }

            // Kazıma başarısız olursa GenelPara Döviz yöntemine Fallback
            return await FetchFromGenelParaAsync(symbol, "doviz");
        }

        /// <summary>
        /// Kategoriye göre anlık kullanılabilir sembol listesini çeker (Örn. Hisse -> borsa.doviz.com/hisseler).
        /// </summary>
        public static async Task<List<SymbolItem>> GetAvailableSymbolsAsync(string? category)
        {
            var results = new List<SymbolItem>();
            string cat = (category ?? "").Trim();

            try
            {
                if (cat == "Hisse Senedi" || cat == "Hisse")
                {
                    using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://borsa.doviz.com/hisseler");
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0)");
                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        string html = await response.Content.ReadAsStringAsync();
                        // <tr id="FENER" data-sector="78" data-name="FENER - FENERBAHCE FUTBOL">
                        var regex = new System.Text.RegularExpressions.Regex(@"<tr[^>]*data-name=""([^-]+)\s*-\s*([^""]+)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        foreach (System.Text.RegularExpressions.Match match in regex.Matches(html))
                        {
                            results.Add(new SymbolItem { Symbol = match.Groups[1].Value.Trim(), Name = match.Groups[2].Value.Trim() });
                        }
                    }
                }
                else if (cat == "Altın")
                {
                    // Altınlar borsa.doviz.com'daki altin sayfası. Sabit liste yerine sayfadan çekebiliriz, 
                    // Ancak sembolleri sistemle uyumlu tutmak için manuel eklemek daha güvenlidir, 
                    // yinede Doviz.com html'sinden adları alabiliriz.
                    using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://altin.doviz.com/");
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0)");
                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        string html = await response.Content.ReadAsStringAsync();
                        // <a href="https://altin.doviz.com/gram-altin">Gram Altın</a>
                        // veya data-socket-key="gram-altin" satırları.. Sistem XAU, CAU, YAU vb kullanıyor.
                        results.Add(new SymbolItem { Symbol = "XAU", Name = "Gram Altın" });
                        results.Add(new SymbolItem { Symbol = "CAU", Name = "Çeyrek Altın" });
                        results.Add(new SymbolItem { Symbol = "YAU", Name = "Yarım Altın" });
                        results.Add(new SymbolItem { Symbol = "TAU", Name = "Tam Altın" });
                        results.Add(new SymbolItem { Symbol = "CUMHURIYET", Name = "Cumhuriyet Altını" });
                        results.Add(new SymbolItem { Symbol = "ATA", Name = "Ata Altın" });
                        results.Add(new SymbolItem { Symbol = "ONS", Name = "Ons Altın" });
                    }
                }
                else if (cat == "Döviz")
                {
                    using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://kur.doviz.com/");
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0)");
                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        string html = await response.Content.ReadAsStringAsync();
                        // <td class="text-bold" data-socket-key="USD" ...>...</td> 
                        // <td class="text-bold" data-socket-key="EUR" ...>...</td>
                        // Maalesef sayfada isimleri (Amerikan Doları) kolayca regexle çekmek zor olabilir. Sabit sık kullanılanları dönelim.
                        results.Add(new SymbolItem { Symbol = "USD", Name = "Amerikan Doları" });
                        results.Add(new SymbolItem { Symbol = "EUR", Name = "Euro" });
                        results.Add(new SymbolItem { Symbol = "GBP", Name = "İngiliz Sterlini" });
                        results.Add(new SymbolItem { Symbol = "CHF", Name = "İsviçre Frangı" });
                        results.Add(new SymbolItem { Symbol = "CAD", Name = "Kanada Doları" });
                        results.Add(new SymbolItem { Symbol = "AUD", Name = "Avustralya Doları" });
                        results.Add(new SymbolItem { Symbol = "JPY", Name = "Japon Yeni" });
                    }
                }
                else if (cat == "Kripto Para" || cat == "Kripto")
                {
                    results.Add(new SymbolItem { Symbol = "BTC-USD", Name = "Bitcoin" });
                    results.Add(new SymbolItem { Symbol = "ETH-USD", Name = "Ethereum" });
                    results.Add(new SymbolItem { Symbol = "BNB-USD", Name = "BNB" });
                    results.Add(new SymbolItem { Symbol = "XRP-USD", Name = "Ripple" });
                    results.Add(new SymbolItem { Symbol = "SOL-USD", Name = "Solana" });
                    results.Add(new SymbolItem { Symbol = "ADA-USD", Name = "Cardano" });
                    results.Add(new SymbolItem { Symbol = "AVAX-USD", Name = "Avalanche" });
                    results.Add(new SymbolItem { Symbol = "DOGE-USD", Name = "Dogecoin" });
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"GetAvailableSymbolsAsync error: {ex.Message}");
            }

            return results;
        }
    }
}
