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
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var searchResponse = JsonSerializer.Deserialize<GitHubSearchResponse>(json);
                
                if (searchResponse?.Items != null)
                {
                    foreach (var item in searchResponse.Items)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;
                        
                        var confidence = CalculateConfidence(item);
                        var risk = EstimateRisk(item);
                        
                        results.Add(ResearchResult.Create(
                            title: item.Name,
                            source: "GitHub",
                            url: item.HtmlUrl,
                            snippet: item.TextMatches?.FirstOrDefault()?.Fragment ?? item.Path,
                            confidence: confidence,
                            estimatedRisk: risk
                        ));
                    }
                }
            }
            else
            {
                _logger?.Warning($"GitHub API error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"GitHub search failed: {ex.Message}", ex);
        }
        
        return results;
    }
    
    private string BuildSearchQuery(string query, UsbDeviceInfo? deviceInfo)
    {
        var parts = new List<string> { query };
        
        if (deviceInfo != null)
        {
            if (!string.IsNullOrEmpty(deviceInfo.Manufacturer))
                parts.Add($"org:{deviceInfo.Manufacturer}");
            
            if (!string.IsNullOrEmpty(deviceInfo.VidPid))
                parts.Add(deviceInfo.VidPid);
        }
        
        // Focus on relevant file types
        parts.Add("extension:c extension:cpp extension:py extension:sh extension:md extension:txt");
        
        return string.Join(" ", parts);
    }
    
    private double CalculateConfidence(GitHubSearchItem item)
    {
        var confidence = 0.5;
        
        if (item.Repository?.StargazersCount > 100)
            confidence += 0.2;
        else if (item.Repository?.StargazersCount > 10)
            confidence += 0.1;
        
        if (item.Repository?.UpdatedAt > DateTime.UtcNow.AddYears(-2))
            confidence += 0.1;
        
        return Math.Min(confidence, 1.0);
    }
    
    private RiskLevel EstimateRisk(GitHubSearchItem item)
    {
        var path = item.Path.ToLower();
        var name = item.Name.ToLower();
        
        if (path.Contains("exploit") || path.Contains("root") || path.Contains("unlock") || 
            name.Contains("exploit") || name.Contains("root") || name.Contains("unlock"))
        {
            return RiskLevel.PotentialBrick;
        }
        
        if (path.Contains("firmware") || path.Contains("flash") || 
            name.Contains("firmware") || name.Contains("flash"))
        {
            return RiskLevel.PersistentWrite;
        }
        
        if (path.Contains("bootloader") || path.Contains("recovery") || 
            name.Contains("bootloader") || name.Contains("recovery"))
        {
            return RiskLevel.Reversible;
        }
        
        return RiskLevel.ReadOnly;
    }
    
    public override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("https://api.github.com", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class GitHubSearchResponse
{
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("items")]
    public IReadOnlyList<GitHubSearchItem>? Items { get; set; }
}

internal sealed class GitHubSearchItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
    
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;
    
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("repository")]
    public GitHubRepository? Repository { get; set; }
    
    [JsonPropertyName("text_matches")]
    public IReadOnlyList<GitHubTextMatch>? TextMatches { get; set; }
}

internal sealed class GitHubRepository
{
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;
    
    [JsonPropertyName("stargazers_count")]
    public int StargazersCount { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

internal sealed class GitHubTextMatch
{
    [JsonPropertyName("fragment")]
    public string Fragment { get; set; } = string.Empty;
}
