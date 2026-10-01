using OpenDeviceToolkit.Core;
using OpenDeviceToolkit.Core.Research;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace OpenDeviceToolkit.Tests.Research;

public class ExploitDbSearchTests
{
    private readonly AppLogger _logger = new(new Workspace());
    
    [Fact]
    public void Name_ReturnsExploitDb()
    {
        var httpClient = new HttpClient();
        var search = new ExploitDbSearch(httpClient, _logger);
        Assert.Equal("Exploit-DB", search.Name);
    }
    
    [Fact]
    public void Priority_Returns30()
    {
        var httpClient = new HttpClient();
        var search = new ExploitDbSearch(httpClient, _logger);
        Assert.Equal(30, search.Priority);
    }
    
    [Fact]
    public void IsCveQuery_DetectsValidCveIds()
    {
        var httpClient = new HttpClient();
        var search = new ExploitDbSearch(httpClient, _logger);
        
        // Use reflection to access private method
        var method = typeof(ExploitDbSearch).GetMethod("IsCveQuery", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        // Test valid CVE IDs
        Assert.True((bool)method.Invoke(search, new object[] { "CVE-2021-1234" }));
        Assert.True((bool)method.Invoke(search, new object[] { "CVE-2020-56789" }));
        Assert.True((bool)method.Invoke(search, new object[] { "CVE-1999-1" }));
        
        // Test invalid formats
        Assert.False((bool)method.Invoke(search, new object[] { "not a cve" }));
        Assert.False((bool)method.Invoke(search, new object[] { "CVE-2021" }));
        Assert.False((bool)method.Invoke(search, new object[] { "CVE-2021-12345678" }));
    }
    
    [Fact]
    public void BuildSearchQuery_IncludesDeviceInfo()
    {
        var httpClient = new HttpClient();
        var search = new ExploitDbSearch(httpClient, _logger);
        
        var method = typeof(ExploitDbSearch).GetMethod("BuildSearchQuery", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var deviceInfo = new UsbDeviceInfo
        {
            Manufacturer = "TestManufacturer",
            VendorId = 0x1234,
            ProductId = 0x5678
        };
        
        var query = (string)method.Invoke(search, new object[] { "test query", deviceInfo });
        
        Assert.Contains("test query", query);
        Assert.Contains("TestManufacturer", query);
        Assert.Contains("VID_1234", query);
        Assert.Contains("PID_5678", query);
    }
    
    [Fact]
    public void CalculateConfidence_ReturnsValidScores()
    {
        var httpClient = new HttpClient();
        var search = new ExploitDbSearch(httpClient, _logger);
        
        var method = typeof(ExploitDbSearch).GetMethod("CalculateConfidence", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        // Create a test exploit with various properties
        var exploit = new {
            Cve = "CVE-2021-1234",
            Verified = true,
            Type = "remote"
        };
        
        // Use dynamic to create ExploitDbExploit
        var exploitType = typeof(ExploitDbSearch).GetNestedType("ExploitDbExploit", 
            System.Reflection.BindingFlags.NonPublic);
        
        Assert.NotNull(exploitType);
        var exploitInstance = Activator.CreateInstance(exploitType);
        
        exploitType.GetProperty("Cve").SetValue(exploitInstance, "CVE-2021-1234");
        exploitType.GetProperty("Verified").SetValue(exploitInstance, true);
        exploitType.GetProperty("Type").SetValue(exploitInstance, "remote");
        
        var confidence = (double)method.Invoke(search, new object[] { exploitInstance });
        
        // Should be between 0 and 1
        Assert.InRange(confidence, 0.0, 1.0);
        // With CVE + Verified + remote, should be high
        Assert.True(confidence > 0.5);
    }
    
    [Fact]
    public void EstimateRisk_ClassifiesCorrectly()
    {
        var httpClient = new HttpClient();
        var search = new ExploitDbSearch(httpClient, _logger);
        
        var method = typeof(ExploitDbSearch).GetMethod("EstimateRisk", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var exploitType = typeof(ExploitDbSearch).GetNestedType("ExploitDbExploit", 
            System.Reflection.BindingFlags.NonPublic);
        
        Assert.NotNull(exploitType);
        
        // Test DoS classification
        var dosExploit = Activator.CreateInstance(exploitType);
        exploitType.GetProperty("Type").SetValue(dosExploit, "dos");
        var dosRisk = (RiskLevel)method.Invoke(search, new object[] { dosExploit });
        Assert.Equal(RiskLevel.PotentialBrick, dosRisk);
        
        // Test remote classification
        var remoteExploit = Activator.CreateInstance(exploitType);
        exploitType.GetProperty("Type").SetValue(remoteExploit, "remote");
        var remoteRisk = (RiskLevel)method.Invoke(search, new object[] { remoteExploit });
        Assert.Equal(RiskLevel.PersistentWrite, remoteRisk);
        
        // Test webapps classification
        var webExploit = Activator.CreateInstance(exploitType);
        exploitType.GetProperty("Type").SetValue(webExploit, "webapps");
        var webRisk = (RiskLevel)method.Invoke(search, new object[] { webExploit });
        Assert.Equal(RiskLevel.Reversible, webRisk);
    }
}

public class GitHubSearchTests
{
    private readonly AppLogger _logger = new(new Workspace());
    
    [Fact]
    public void Name_ReturnsGitHub()
    {
        var httpClient = new HttpClient();
        var search = new GitHubSearch(httpClient, _logger);
        Assert.Equal("GitHub", search.Name);
    }
    
    [Fact]
    public void Priority_Returns10()
    {
        var httpClient = new HttpClient();
        var search = new GitHubSearch(httpClient, _logger);
        Assert.Equal(10, search.Priority);
    }
    
    [Fact]
    public void BuildSearchQuery_IncludesDeviceInfo()
    {
        var httpClient = new HttpClient();
        var search = new GitHubSearch(httpClient, _logger);
        
        var method = typeof(GitHubSearch).GetMethod("BuildSearchQuery", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var deviceInfo = new UsbDeviceInfo
        {
            Manufacturer = "TestManufacturer",
            ProductName = "TestProduct",
            VendorId = 0x1234,
            ProductId = 0x5678
        };
        
        var query = (string)method.Invoke(search, new object[] { "test query", deviceInfo });
        
        Assert.Contains("test query", query);
        Assert.Contains("TestManufacturer", query);
        Assert.Contains("TestProduct", query);
    }
    
    [Fact]
    public void CalculateConfidence_ReturnsValidScores()
    {
        var httpClient = new HttpClient();
        var search = new GitHubSearch(httpClient, _logger);
        
        var method = typeof(GitHubSearch).GetMethod("CalculateConfidence", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        // Create a test item
        var itemType = typeof(GitHubSearch).GetNestedType("GitHubSearchItem", 
            System.Reflection.BindingFlags.NonPublic);
        
        Assert.NotNull(itemType);
        var item = Activator.CreateInstance(itemType);
        
        itemType.GetProperty("Name").SetValue(item, "test exploit");
        itemType.GetProperty("Path").SetValue(item, "path/to/file");
        
        var confidence = (double)method.Invoke(search, new object[] { item });
        
        // Should be between 0 and 1
        Assert.InRange(confidence, 0.0, 1.0);
    }
    
    [Fact]
    public void EstimateRisk_ReturnsRiskLevel()
    {
        var httpClient = new HttpClient();
        var search = new GitHubSearch(httpClient, _logger);
        
        var method = typeof(GitHubSearch).GetMethod("EstimateRisk", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var itemType = typeof(GitHubSearch).GetNestedType("GitHubSearchItem", 
            System.Reflection.BindingFlags.NonPublic);
        
        Assert.NotNull(itemType);
        var item = Activator.CreateInstance(itemType);
        
        itemType.GetProperty("Name").SetValue(item, "test");
        
        var risk = (RiskLevel)method.Invoke(search, new object[] { item });
        
        // Should return a valid RiskLevel
        Assert.True(Enum.IsDefined(typeof(RiskLevel), risk));
    }
}

public class XdaSearchTests
{
    private readonly AppLogger _logger = new(new Workspace());
    
    [Fact]
    public void Name_ReturnsXdaDevelopersForum()
    {
        var httpClient = new HttpClient();
        var search = new XdaSearch(httpClient, _logger);
        Assert.Equal("XDA Developers Forum", search.Name);
    }
    
    [Fact]
    public void Priority_Returns20()
    {
        var httpClient = new HttpClient();
        var search = new XdaSearch(httpClient, _logger);
        Assert.Equal(20, search.Priority);
    }
    
    [Fact]
    public void BuildSearchUrl_IncludesQuery()
    {
        var httpClient = new HttpClient();
        var search = new XdaSearch(httpClient, _logger);
        
        var method = typeof(XdaSearch).GetMethod("BuildSearchUrl", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var url = (string)method.Invoke(search, new object[] { "test query", null });
        
        Assert.Contains("test query", url);
        Assert.Contains("forum.xda-developers.com", url);
    }
    
    [Fact]
    public void BuildSearchUrl_IncludesDeviceInfo()
    {
        var httpClient = new HttpClient();
        var search = new XdaSearch(httpClient, _logger);
        
        var method = typeof(XdaSearch).GetMethod("BuildSearchUrl", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var deviceInfo = new UsbDeviceInfo
        {
            Manufacturer = "TestManufacturer"
        };
        
        var url = (string)method.Invoke(search, new object[] { "test query", deviceInfo });
        
        Assert.Contains("test query", url);
        Assert.Contains("TestManufacturer", url);
    }
    
    [Fact]
    public void EstimateRisk_ClassifiesRootAsPotentialBrick()
    {
        var httpClient = new HttpClient();
        var search = new XdaSearch(httpClient, _logger);
        
        var method = typeof(XdaSearch).GetMethod("EstimateRisk", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var risk = (RiskLevel)method.Invoke(search, new object[] { "How to ROOT your device" });
        Assert.Equal(RiskLevel.PotentialBrick, risk);
    }
    
    [Fact]
    public void EstimateRisk_ClassifiesFirmwareAsPersistentWrite()
    {
        var httpClient = new HttpClient();
        var search = new XdaSearch(httpClient, _logger);
        
        var method = typeof(XdaSearch).GetMethod("EstimateRisk", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var risk = (RiskLevel)method.Invoke(search, new object[] { "Custom ROM firmware flash" });
        Assert.Equal(RiskLevel.PersistentWrite, risk);
    }
    
    [Fact]
    public void EstimateRisk_ClassifiesRecoveryAsReversible()
    {
        var httpClient = new HttpClient();
        var search = new XdaSearch(httpClient, _logger);
        
        var method = typeof(XdaSearch).GetMethod("EstimateRisk", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var risk = (RiskLevel)method.Invoke(search, new object[] { "Recovery mode guide" });
        Assert.Equal(RiskLevel.Reversible, risk);
    }
    
    [Fact]
    public void EstimateRisk_ClassifiesUnknownAsReadOnly()
    {
        var httpClient = new HttpClient();
        var search = new XdaSearch(httpClient, _logger);
        
        var method = typeof(XdaSearch).GetMethod("EstimateRisk", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var risk = (RiskLevel)method.Invoke(search, new object[] { "Some random topic" });
        Assert.Equal(RiskLevel.ReadOnly, risk);
    }
}

public class OfflineResearcherTests
{
    private readonly AppLogger _logger = new(new Workspace());
    
    [Fact]
    public void Name_ReturnsOfflineFingerprinting()
    {
        var researcher = new OfflineResearcher(_logger);
        Assert.Equal("Offline Fingerprinting", researcher.Name);
    }
    
    [Fact]
    public void Priority_Returns5()
    {
        var researcher = new OfflineResearcher(_logger);
        Assert.Equal(5, researcher.Priority);
    }
    
    [Fact]
    public async Task IsAvailableAsync_ReturnsTrue()
    {
        var researcher = new OfflineResearcher(_logger);
        var available = await researcher.IsAvailableAsync(CancellationToken.None);
        Assert.True(available);
    }
    
    [Fact]
    public async Task SearchAsync_QualcommQuery_ReturnsEdlResults()
    {
        var researcher = new OfflineResearcher(_logger);
        var results = await researcher.SearchAsync("qualcomm edl", null, CancellationToken.None);
        
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Title.Contains("EDL"));
    }
    
    [Fact]
    public async Task SearchAsync_UnlockQuery_ReturnsFastbootResults()
    {
        var researcher = new OfflineResearcher(_logger);
        var results = await researcher.SearchAsync("unlock bootloader", null, CancellationToken.None);
        
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Title.Contains("Fastboot"));
    }
    
    [Fact]
    public async Task SearchAsync_DirtyCowQuery_ReturnsExploitResults()
    {
        var researcher = new OfflineResearcher(_logger);
        var results = await researcher.SearchAsync("dirtycow", null, CancellationToken.None);
        
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Title.Contains("DirtyCow"));
    }
    
    [Fact]
    public async Task SearchAsync_LgDeviceInfo_ReturnsLgResults()
    {
        var researcher = new OfflineResearcher(_logger);
        var deviceInfo = new UsbDeviceInfo
        {
            Manufacturer = "LG",
            DisplayName = "LG-V450"
        };
        
        var results = await researcher.SearchAsync("exploit", deviceInfo, CancellationToken.None);
        
        Assert.NotEmpty(results);
        // Should return LG-specific results
        Assert.Contains(results, r => r.Source == "Offline DB");
    }
    
    [Fact]
    public async Task SearchAsync_QualcommVidPid_ReturnsQualcommResults()
    {
        var researcher = new OfflineResearcher(_logger);
        var deviceInfo = new UsbDeviceInfo
        {
            VidPid = "VID_05C6_PID_9008"
        };
        
        var results = await researcher.SearchAsync("device", deviceInfo, CancellationToken.None);
        
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Snippet.Contains("Qualcomm"));
    }
    
    [Fact]
    public async Task SearchAsync_LgVidPid_ReturnsLgResults()
    {
        var researcher = new OfflineResearcher(_logger);
        var deviceInfo = new UsbDeviceInfo
        {
            VidPid = "VID_1004"
        };
        
        var results = await researcher.SearchAsync("device", deviceInfo, CancellationToken.None);
        
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Snippet.Contains("LG"));
    }
}

public class ResearchSourceIntegrationTests
{
    private readonly Workspace _workspace = new();
    private readonly AppLogger _logger = new(_workspace);
    private readonly CommandRunner _runner = new();
    
    [Fact]
    public void ResearchEngine_IncludesAllSources()
    {
        var engine = new ResearchEngine(_workspace, _logger, _runner);
        var sources = engine.Sources;
        
        Assert.Contains(sources, s => s.Name == "GitHub");
        Assert.Contains(sources, s => s.Name == "XDA Developers Forum");
        Assert.Contains(sources, s => s.Name == "Exploit-DB");
        Assert.Contains(sources, s => s.Name == "Offline Fingerprinting");
    }
    
    [Fact]
    public void ResearchEngine_SourcesAreOrderedByPriority()
    {
        var engine = new ResearchEngine(_workspace, _logger, _runner);
        var sources = engine.Sources;
        
        // Check that sources are in descending priority order
        for (int i = 0; i < sources.Count - 1; i++)
        {
            Assert.True(sources[i].Priority >= sources[i + 1].Priority,
                $"Source {sources[i].Name} (priority {sources[i].Priority}) should come before {sources[i+1].Name} (priority {sources[i+1].Priority})");
        }
    }
    
    [Fact]
    public void ResearchEngine_CanAddAndRemoveSources()
    {
        var engine = new ResearchEngine(_workspace, _logger, _runner);
        var initialCount = engine.Sources.Count;
        
        var mockSource = new Mock<IResearchSource>();
        mockSource.Setup(s => s.Name).Returns("MockSource");
        mockSource.Setup(s => s.Priority).Returns(0);
        mockSource.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<UsbDeviceInfo?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ResearchResult>().AsReadOnly());
        mockSource.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        engine.AddSource(mockSource.Object);
        Assert.Equal(initialCount + 1, engine.Sources.Count);
        
        engine.RemoveSource(mockSource.Object);
        Assert.Equal(initialCount, engine.Sources.Count);
    }
}
