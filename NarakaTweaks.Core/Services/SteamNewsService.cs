using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NarakaTweaks.Core.Services
{
    public class SteamNewsService
    {
        private readonly HttpClient _httpClient;
        private const string STEAM_NEWS_API = "https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/";
        private const int NARAKA_APP_ID = 1203220;
        
        public SteamNewsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        public async Task<List<NewsArticle>> GetNewsAsync(int count = 5)
        {
            try
            {
                var url = $"{STEAM_NEWS_API}?appid={NARAKA_APP_ID}&count={count}";
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    return GetFallbackNews();
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var newsResponse = JsonSerializer.Deserialize<SteamNewsResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (newsResponse?.AppNews?.NewsItems == null || !newsResponse.AppNews.NewsItems.Any())
                {
                    return GetFallbackNews();
                }
                
                return newsResponse.AppNews.NewsItems
                    .Take(count)
                    .Select(item => new NewsArticle
                    {
                        Title = CleanTitle(item.Title),
                        Description = ExtractDescription(item.Contents),
                        Url = item.Url,
                        Date = DateTimeOffset.FromUnixTimeSeconds(item.Date).DateTime,
                        Author = item.Author
                    })
                    .ToList();
            }
            catch
            {
                return GetFallbackNews();
            }
        }
        
        private string CleanTitle(string title)
        {
            // Remove common prefixes
            title = title.Replace("NARAKA: BLADEPOINT Update – ", "")
                        .Replace("NARAKA: BLADEPOINT ", "")
                        .Replace("Update – ", "");
            
            // Limit length
            if (title.Length > 60)
            {
                title = title.Substring(0, 57) + "...";
            }
            
            return title;
        }
        
        private string ExtractDescription(string contents)
        {
            if (string.IsNullOrWhiteSpace(contents))
                return "Click to read more...";
            
            // Remove BBCode tags
            var cleaned = contents
                .Replace("[p]", "")
                .Replace("[/p]", " ")
                .Replace("[h1]", "")
                .Replace("[/h1]", " ")
                .Replace("[b]", "")
                .Replace("[/b]", "")
                .Replace("[i]", "")
                .Replace("[/i]", "")
                .Replace("[table", "")
                .Replace("[/table]", "")
                .Replace("[tr]", "")
                .Replace("[/tr]", "")
                .Replace("[td]", "")
                .Replace("[/td]", "")
                .Replace("  ", " ")
                .Trim();
            
            // Get first sentence or 150 characters
            var firstPeriod = cleaned.IndexOf('.');
            if (firstPeriod > 0 && firstPeriod < 200)
            {
                return cleaned.Substring(0, firstPeriod + 1).Trim();
            }
            
            if (cleaned.Length > 150)
            {
                return cleaned.Substring(0, 147) + "...";
            }
            
            return cleaned;
        }
        
        private List<NewsArticle> GetFallbackNews()
        {
            return new List<NewsArticle>
            {
                new NewsArticle
                {
                    Title = "🎮 Season Update Available",
                    Description = "New season content now live with exclusive rewards!",
                    Date = DateTime.Now.AddDays(-1)
                },
                new NewsArticle
                {
                    Title = "⚔️ Balance Changes",
                    Description = "Hero adjustments and weapon tuning in latest patch.",
                    Date = DateTime.Now.AddDays(-3)
                },
                new NewsArticle
                {
                    Title = "🎉 Community Event",
                    Description = "Join the weekend tournament for special prizes!",
                    Date = DateTime.Now.AddDays(-5)
                },
                new NewsArticle
                {
                    Title = "🔧 Performance Improvements",
                    Description = "Latest update includes optimization fixes.",
                    Date = DateTime.Now.AddDays(-7)
                }
            };
        }
    }
    
    public class NewsArticle
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Url { get; set; }
        public DateTime Date { get; set; }
        public string? Author { get; set; }
    }
    
    // JSON deserialization classes
    internal class SteamNewsResponse
    {
        public AppNewsContainer? AppNews { get; set; }
    }
    
    internal class AppNewsContainer
    {
        public int AppId { get; set; }
        public List<SteamNewsItem>? NewsItems { get; set; }
        public int Count { get; set; }
    }
    
    internal class SteamNewsItem
    {
        public string Gid { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool Is_External_Url { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Contents { get; set; } = string.Empty;
        public string Feedlabel { get; set; } = string.Empty;
        public long Date { get; set; }
        public string Feedname { get; set; } = string.Empty;
        public int Feed_Type { get; set; }
        public int AppId { get; set; }
    }
}
