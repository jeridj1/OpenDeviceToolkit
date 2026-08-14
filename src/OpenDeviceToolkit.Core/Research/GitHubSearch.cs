using System.Text.Json;

namespace OpenDeviceToolkit.Core.Research;

/// <summary>
/// Searches GitHub for exploits, datasheets, and code related to device research.
/// </summary>
public sealed class GitHubSearch : ResearchSourceBase
{
    private readonly HttpClient _httpClient;
    private readonly AppLogger? _logger;
    
    public override string Name => "GitHub";
    public override int Priority => 10; // High priority
    
    public GitHubSearch(HttpClient httpClient, AppLogger? logger = null)
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
            // Build GitHub search query
            var searchQuery = BuildSearchQuery(query, deviceInfo);
            var url = $"https://api.github.com/search/code?q={Uri.EscapeDataString(searchQuery)}&per_page=10";
            
            _logger?.Info($