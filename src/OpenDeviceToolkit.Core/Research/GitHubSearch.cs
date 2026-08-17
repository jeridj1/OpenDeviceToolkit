using System.Text.Json;
using System.Text.Json.Serialization;
using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Core.Research;

/// <summary>
/// Searches GitHub for exploits, datasheets, and code related to device research.
/// </summary>
public sealed class GitHubSearch : ResearchSourceBase
{
    private readonly HttpClient _httpClient;
    private readonly AppLogger? _logger;
    
    public override string Name => "GitHub";
    public override int Priority => 10;
    
    public GitHubSearch(HttpClient httpClient, AppLogger? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("OpenDeviceToolkit/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }
    
    public override async Task<IReadOnlyList<ResearchResult>> SearchAsync(
        string query,
        UsbDeviceInfo? deviceInfo = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ResearchResult>();
        
        try
        {
            var searchQuery = BuildSearchQuery(query, deviceInfo);
            var url = $"https://api.github.com/search/code?q={Uri.EscapeDataString(searchQuery)}&per_page=10";
            
            _logger?.Info($"Searching GitHub for: {searchQuery}");
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return results;
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<GitHubSearchResponse>(json);
            if (data?.Items == null) return results;
            
            foreach (var item in data.Items)
            {
                results.Add(ResearchResult.Create(
                    item.Name ?? "GitHub Result",
                    "GitHub Code Search",
                    item.HtmlUrl ?? "",
                    confidence: 0.8,
                    estimatedRisk: EstimateRisk(item.Name ?? ""),
                    tags: new[] { "github", "code" }));
            }
        }
        catch (Exception ex) { _logger?.Error($"GitHub search failed: {ex.Message}", ex); }
        
        return results;
    }
    
    private string BuildSearchQuery(string query, UsbDeviceInfo? deviceInfo)
    {
        var parts = new List<string> { query };
        if (deviceInfo?.Manufacturer != null) parts.Add(deviceInfo.Manufacturer);
        if (deviceInfo?.Name != null) parts.Add(deviceInfo.Name);
        return string.Join(" ", parts);
    }
    
    private static RiskLevel EstimateRisk(string name) => name.ToLowerInvariant() switch
    {
        var n when n.Contains("exploit") || n.Contains("root") || n.Contains("unlock") => RiskLevel.PotentialBrick,
        var n when n.Contains("flash") || n.Contains("firmware") => RiskLevel.PersistentWrite,
        var n when n.Contains("recovery") || n.Contains("boot") => RiskLevel.Reversible,
        _ => RiskLevel.ReadOnly
    };

    public override Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    private sealed class GitHubSearchResponse
    {
        [JsonPropertyName("items")]
        public List<GitHubSearchItem>? Items { get; set; }
    }

    private sealed class GitHubSearchItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }
    }
}
