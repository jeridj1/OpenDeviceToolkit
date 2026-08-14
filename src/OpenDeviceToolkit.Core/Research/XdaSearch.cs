using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenDeviceToolkit.Core.Research;

/// <summary>
/// Searches XDA Developers Forum for device-specific information, ROMs, exploits, and guides.
/// </summary>
public sealed class XdaSearch : ResearchSourceBase
{
    private readonly HttpClient _httpClient;
    private readonly AppLogger? _logger;
    
    public override string Name => "XDA Developers Forum";
    public override int Priority => 20; // Higher priority than GitHub for Android devices
    
    public XdaSearch(HttpClient httpClient, AppLogger? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("OpenDeviceToolkit/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }
    
    public override async Task<IReadOnlyList<ResearchResult>> SearchAsync(
        string query,
        Usb.UsbDeviceInfo? deviceInfo = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ResearchResult>();
        
        try
        {
            // Build XDA search URL
            var searchUrl = BuildSearchUrl(query, deviceInfo);
            
            _logger?.Info($"Searching XDA for: {query}");
            
            // Note: XDA doesn't have a public API, so we need to scrape or use their search
            // For now, we'll use their search URL and parse results
            // In production, you might need to use a proper scraping library
            
            // Try to get search results page
            var response = await _httpClient.GetAsync(searchUrl, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStringAsync(cancellationToken);
                var extractedResults = ExtractResultsFromHtml(html, searchUrl);
                results.AddRange(extractedResults);
            }
            else
            {
                _logger?.Warning($"XDA search failed with status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"XDA search failed: {ex.Message}", ex);
        }
        
        return results;
    }
    
    private string BuildSearchUrl(string query, Usb.UsbDeviceInfo? deviceInfo)
    {
        var baseUrl = "https://forum.xda-developers.com/search/search";
        var queryParams = new List<string> { $"q={Uri.EscapeDataString(query)}" };
        
        // Add device-specific filters if available
        if (deviceInfo != null)
        {
            if (!string.IsNullOrEmpty(deviceInfo.Manufacturer))
            {
                queryParams.Add($"manufacturer={Uri.EscapeDataString(deviceInfo.Manufacturer)}");
            }
        }
        
        return $"{baseUrl}?{string.Join("&", queryParams)}";
    }
    
    private IReadOnlyList<ResearchResult> ExtractResultsFromHtml(string html, string searchUrl)
    {
        var results = new List<ResearchResult>();
        
        try
        {
            // This is a simplified parser. In production, use HtmlAgilityPack or similar
            // For now, we'll use simple string parsing
            
            // Look for search result links
            var linkPattern = "href=\"/threads/" + "[^\"]*" + ".\"[^\"]*\"";
            var matches = System.Text.RegularExpressions.Regex.Matches(html, linkPattern);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var href = match.Value;
                var urlMatch = System.Text.RegularExpressions.Regex.Match(href, "href=\"(?<url>/threads/[^\"]+)\"");
                if (urlMatch.Success)
                {
                    var threadUrl = $"https://forum.xda-developers.com{urlMatch.Groups["url"].Value}";
                    
                    // Extract title from the link text
                    var titleStart = match.Index + href.Length;
                    var titleEnd = html.IndexOf('<', titleStart);
                    var title = titleEnd > titleStart 
                        ? html.Substring(titleStart, titleEnd - titleStart).Trim()
                        : "XDA Thread";
                    
                    // Clean up title
                    title = System.Web.HttpUtility.HtmlDecode(title);
                    title = System.Text.RegularExpressions.Regex.Replace(title, "<[^>]+>", "");
                    
                    results.Add(ResearchResult.Create(
                        title: title,
                        source: "XDA Developers Forum",
                        url: threadUrl,
                        confidence: 0.8,
                        estimatedRisk: EstimateRisk(title, threadUrl)
                    ));
                }
                
                // Limit results
                if (results.Count >= 10)
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to parse XDA results: {ex.Message}", ex);
        }
        
        return results;
    }
    
    private RiskLevel EstimateRisk(string title, string url)
    {
        var lowerTitle = title.ToLower();
        var lowerUrl = url.ToLower();
        
        if (lowerTitle.Contains("root") || lowerTitle.Contains("unlock") || 
            lowerTitle.Contains("exploit") || lowerTitle.Contains("bypass"))
        {
            return RiskLevel.PotentialBrick;
        }
        
        if (lowerTitle.Contains("rom") || lowerTitle.Contains("firmware") || 
            lowerTitle.Contains("flash") || lowerTitle.Contains("custom"))
        {
            return RiskLevel.PersistentWrite;
        }
        
        if (lowerTitle.Contains("recovery") || lowerTitle.Contains("bootloader") || 
            lowerTitle.Contains("twrp") || lowerTitle.Contains("cwm"))
        {
            return RiskLevel.Reversible;
        }
        
        return RiskLevel.ReadOnly;
    }
    
    public override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test XDA connectivity
            var response = await _httpClient.GetAsync("https://forum.xda-developers.com", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
