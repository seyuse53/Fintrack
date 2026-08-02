using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using FinTrack.Core.Helpers;

namespace FinTrack.Core.Services
{
    public class GitHubReleaseInfo
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public bool IsUpdateAvailable { get; set; }
    }

    public class GitHubReleaseService
    {
        private const string GitHubOwner = "seyuse53";
        private const string GitHubRepo = "Fintrack";
        private const string GitHubApiUrl = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
        
        private readonly HttpClient _httpClient;

        public GitHubReleaseService()
        {
            _httpClient = new HttpClient();
            // GitHub API requires a User-Agent header
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FinTrackUpdater", "1.0"));
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        }

        public async Task<GitHubReleaseInfo> CheckForUpdatesAsync(string currentVersion)
        {
            try
            {
                var response = await _httpClient.GetAsync(GitHubApiUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    // Repo might be private or no releases exist yet
                    return new GitHubReleaseInfo { IsUpdateAvailable = false };
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    var root = doc.RootElement;
                    
                    string tagName = root.GetProperty("tag_name").GetString() ?? "";
                    string body = root.GetProperty("body").GetString() ?? "";
                    DateTime publishedAt = root.GetProperty("published_at").GetDateTime();
                    
                    // Clean up the tag name (e.g. 'v1.0.0' -> '1.0.0')
                    string latestVersion = tagName.TrimStart('v', 'V');
                    string cleanCurrentVersion = currentVersion.TrimStart('v', 'V');
                    
                    // Simple version string comparison or Version class comparison
                    bool isUpdateAvailable = false;
                    if (Version.TryParse(latestVersion, out Version? latest) && 
                        Version.TryParse(cleanCurrentVersion, out Version? current))
                    {
                        isUpdateAvailable = latest > current;
                    }

                    string downloadUrl = string.Empty;
                    
                    // Find the .exe asset URL
                    if (root.TryGetProperty("assets", out JsonElement assets) && assets.GetArrayLength() > 0)
                    {
                        foreach (var asset in assets.EnumerateArray())
                        {
                            string assetName = asset.GetProperty("name").GetString() ?? "";
                            if (assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                                break;
                            }
                        }
                    }

                    return new GitHubReleaseInfo
                    {
                        Version = latestVersion,
                        DownloadUrl = downloadUrl,
                        ReleaseNotes = body,
                        PublishedAt = publishedAt,
                        IsUpdateAvailable = isUpdateAvailable
                    };
                }
            }
            catch (Exception ex)
            {
                // Optionally log the exception
                AppLogger.Error($"Update check failed: {ex.Message}");
                return new GitHubReleaseInfo { IsUpdateAvailable = false };
            }
        }
    }
}
