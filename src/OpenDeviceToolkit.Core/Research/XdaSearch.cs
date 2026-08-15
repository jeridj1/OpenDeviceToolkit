using System.Text.RegularExpressions;

namespace OpenDeviceToolkit.Core.Research;

public sealed class XdaSearch : ResearchSourceBase
{
    private readonly HttpClient _httpClient;
    private readonly AppLogger? _logger;
    public override string Name => "XDA Developers Forum";
    public override int Priority => 20;
    
    public XdaSearch(HttpClient httpClient, AppLogger? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("OpenDeviceToolkit/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }
    
    public override async Task<IReadOnlyList<ResearchResult>> SearchAsync(string query, Usb.UsbDeviceInfo? deviceInfo = null, CancellationToken ct = default)
    {
        var results = new List<ResearchResult>();
        try
        {
            var url = BuildSearchUrl(query, deviceInfo);
            _logger?.Info($"Searching XDA for: {query}");
            var response = await _httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStringAsync(ct);
                results.AddRange(ExtractResultsFromHtml(html, url));
            }
        }
        catch (Exception ex) { _logger?.Error($"XDA search failed: {ex.Message}", ex); }
        return results;
    }
    
    private string BuildSearchUrl(string query, Usb.UsbDeviceInfo? deviceInfo)
    {
        var parts = new List<string> { $"q={Uri.EscapeDataString(query)}" };
        if (deviceInfo?.Manufacturer != null) parts.Add($"manufacturer={Uri.EscapeDataString(deviceInfo.Manufacturer)}");
        return $"https://forum.xda-developers.com/search/search?{string.Join("&", parts)}";
    }
    
    private IReadOnlyList<ResearchResult> ExtractResultsFromHtml(string html, string searchUrl)
    {
        var results = new List<ResearchResult>();
        var pattern = "href=\"/threads/[^\"]+\"";
        foreach (Match match in Regex.Matches(html, pattern))
        {
            var href = match.Value;
            var urlMatch = Regex.Match(href, "href=\"(?<url>/threads/[^\"]+)\"");
            if (urlMatch.Success)
            {
                var threadUrl = $"https://forum.xda-developers.com{urlMatch.Groups["url"].Value}";
                var titleStart = match.Index + href.Length;
                var titleEnd = html.IndexOf('<', titleStart);
                var title = titleEnd > titleStart ? html.Substring(titleStart, titleEnd - titleStart).Trim() : "XDA Thread";
                title = System.Web.HttpUtility.HtmlDecode(title).Replace("<[^>]+", "");
                results.Add(ResearchResult.Create(title, "XDA Developers Forum", threadUrl, confidence: 0.8, estimatedRisk: EstimateRisk(title)));
                if (results.Count >= 10) break;
            }
        }
        return results;
    }
    
    private RiskLevel EstimateRisk(string title) => title.ToLower() switch
    {
        var t when t.Contains("root") || t.Contains("unlock") || t.Contains("exploit") => RiskLevel.PotentialBrick,
        var t when t.Contains("rom") || t.Contains("firmware") || t.Contains("flash") => RiskLevel.PersistentWrite,
        var t when t.Contains("recovery") || t.Contains("bootloader") => RiskLevel.Reversible,
        _ => RiskLevel.ReadOnly
    };
    
    public override async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try { var r = await _httpClient.GetAsync("https://forum.xda-developers.com", ct); return r.IsSuccessStatusCode; }
        catch { return false; }
    }
}