using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinTrack.Core.Services
{
    public class GeminiParsedTransaction
    {
        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("suggestedCategory")]
        public string? SuggestedCategory { get; set; }
    }

    public class GeminiParserService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

        public async Task<List<GeminiParsedTransaction>> ParseStatementAsync(byte[] fileBytes, string fileExtension, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be empty.");

            string mimeType = GetMimeType(fileExtension);
            string base64Data = Convert.ToBase64String(fileBytes);

            string promptText = "Sen finansal ekstreleri analiz eden uzman bir asistansın. " +
                "Sana verilen banka hesabı veya kredi kartı ekstresini/dökümünü (PDF veya görsel) incele ve içerisindeki tüm harcamaları/işlemleri çıkar. " +
                "Yanıtı sadece ve sadece aşağıdaki şemaya uygun, geçerli bir JSON objesi olarak dön. Başka hiçbir açıklama yazma.\n\n" +
                "JSON Şeması:\n" +
                "{\n" +
                "  \"transactions\": [\n" +
                "    {\n" +
                "      \"date\": \"YYYY-MM-DD\", // İşlem tarihi\n" +
                "      \"description\": \"İşlem Açıklaması / İşyeri Adı (kısaltmaları temizle, okunabilir yap)\",\n" +
                "      \"amount\": -125.50, // Harcamalar negatif, iade/ödeme/puan kullanımları pozitif olmalıdır\n" +
                "      \"suggestedCategory\": \"Market & Mutfak\" // Gider türünü en doğru şekilde belirle. Örneğin: BİM, A101, Hakmar, Happy Center, Fırın, Cafe, Pastane, Kahvaltı Sarayı, Hertat vb. yeme-içme ve gıda yerleri için HER ZAMAN tam olarak 'Market & Mutfak' yaz.\n" +
                "    }\n" +
                "  ]\n" +
                "}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = promptText },
                            new
                            {
                                inlineData = new
                                {
                                    mimeType = mimeType,
                                    data = base64Data
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json"
                }
            };

            string requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
            string jsonPayload = JsonSerializer.Serialize(requestBody);

            int maxRetries = 15;
            int delayMs = 2000;
            HttpResponseMessage? response = null;

            for (int i = 0; i < maxRetries; i++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    break;
                }

                if ((int)response.StatusCode == 429 || (int)response.StatusCode == 503)
                {
                    if (i == maxRetries - 1)
                    {
                        break;
                    }
                    
                    int waitTimeMs = delayMs;
                    if (response.Headers.RetryAfter != null && response.Headers.RetryAfter.Delta.HasValue)
                    {
                        waitTimeMs = (int)response.Headers.RetryAfter.Delta.Value.TotalMilliseconds;
                    }
                    else if (response.Headers.RetryAfter != null && response.Headers.RetryAfter.Date.HasValue)
                    {
                        waitTimeMs = (int)(response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow).TotalMilliseconds;
                    }

                    if (waitTimeMs <= 0) waitTimeMs = delayMs;
                    
                    await Task.Delay(waitTimeMs);
                    delayMs = Math.Min(delayMs * 2, 30000); // Max delay of 30 seconds per retry
                }
                else
                {
                    break;
                }
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                string errorContent = response != null ? await response.Content.ReadAsStringAsync() : "Unknown error";

                if (response != null && ((int)response.StatusCode == 503 || (int)response.StatusCode == 429))
                {
                    throw new Exception("Google Gemini sunucuları şu an çok yoğun. Lütfen birkaç dakika bekleyip tekrar deneyin.");
                }

                throw new HttpRequestException($"Gemini API error ({(response?.StatusCode.ToString() ?? "Unknown")}): {errorContent}");
            }

            string responseJson = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() == 0)
                    return new List<GeminiParsedTransaction>();

                var firstCandidate = candidates[0];
                var content = firstCandidate.GetProperty("content");
                var parts = content.GetProperty("parts");
                if (parts.GetArrayLength() == 0)
                    return new List<GeminiParsedTransaction>();

                var text = parts[0].GetProperty("text").GetString();
                if (string.IsNullOrWhiteSpace(text))
                    return new List<GeminiParsedTransaction>();

                // LLM'in fazladan ekleyebileceği markdown veya düz metinleri temizle
                string cleanText = text.Trim();
                
                int firstBrace = cleanText.IndexOf('{');
                int firstBracket = cleanText.IndexOf('[');
                int firstChar = -1;
                if (firstBrace >= 0 && firstBracket >= 0) firstChar = Math.Min(firstBrace, firstBracket);
                else if (firstBrace >= 0) firstChar = firstBrace;
                else if (firstBracket >= 0) firstChar = firstBracket;

                int lastBrace = cleanText.LastIndexOf('}');
                int lastBracket = cleanText.LastIndexOf(']');
                int lastChar = Math.Max(lastBrace, lastBracket);
                
                if (firstChar >= 0 && lastChar > firstChar)
                {
                    cleanText = cleanText.Substring(firstChar, lastChar - firstChar + 1);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                if (cleanText.StartsWith("["))
                {
                    var list = JsonSerializer.Deserialize<List<GeminiParsedTransaction>>(cleanText, options);
                    return list ?? new List<GeminiParsedTransaction>();
                }
                else
                {
                    var parsedResponse = JsonSerializer.Deserialize<GeminiParsedResponse>(cleanText, options);
                    return parsedResponse?.Transactions ?? new List<GeminiParsedTransaction>();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse Gemini API response. Make sure the output is a valid JSON.", ex);
            }
        }

        private string GetMimeType(string fileExtension)
        {
            switch (fileExtension.ToLower().TrimStart('.'))
            {
                case "pdf": return "application/pdf";
                case "png": return "image/png";
                case "jpg":
                case "jpeg": return "image/jpeg";
                default:
                    throw new NotSupportedException($"File type '.{fileExtension}' is not supported. Use PDF, PNG, or JPG.");
            }
        }

        private class GeminiParsedResponse
        {
            [JsonPropertyName("transactions")]
            public List<GeminiParsedTransaction>? Transactions { get; set; }
        }
    }
}
