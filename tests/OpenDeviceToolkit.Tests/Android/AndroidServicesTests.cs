using OpenDeviceToolkit.Core;
using OpenDeviceToolkit.Android;
using Moq;

namespace OpenDeviceToolkit.Tests.Android;

public class AndroidDeviceTests
{
    [Fact]
    public void Create_WithValidProperties()
    {
        var device = new AndroidDevice("test-serial", "TestModel", "TestManufacturer");
        
        Assert.Equal("test-serial", device.SerialNumber);
        Assert.Equal("TestModel", device.Model);
        Assert.Equal("TestManufacturer", device.Manufacturer);
        Assert.NotNull(device.Properties);
        Assert.Empty(device.Properties);
    }
    
    [Fact]
    public void AddProperty_AddsToDictionary()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        device.AddProperty("ro.build.version.sdk", "30");
        
        Assert.Single(device.Properties);
        Assert.Equal("30", device.Properties["ro.build.version.sdk"]);
    }
    
    [Fact]
    public void GetProperty_ReturnsValue()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        device.AddProperty("ro.product.cpu.abi", "arm64-v8a");
        
        var value = device.GetProperty("ro.product.cpu.abi");
        Assert.Equal("arm64-v8a", value);
    }
    
    [Fact]
    public void GetProperty_ReturnsNullForMissing()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        
        var value = device.GetProperty("nonexistent");
        Assert.Null(value);
    }
    
    [Fact]
    public void DisplayName_ReturnsModelAndManufacturer()
    {
        var device = new AndroidDevice("serial", "Pixel 6", "Google");
        
        Assert.Equal("Google Pixel 6", device.DisplayName);
    }
}

public class AndroidCapabilityAnalyzerTests
{
    [Fact]
    public void Analyze_EmptyProperties_ReturnsNoCapabilities()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        var capabilities = AndroidCapabilityAnalyzer.Analyze(device);
        
        Assert.Empty(capabilities);
    }
    
    [Fact]
    public void Analyze_AdbEnabled_ReturnsAdbCapabilities()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        device.AddProperty("ro.debuggable", "1");
        device.AddProperty("ro.secure", "0");
        
        var capabilities = AndroidCapabilityAnalyzer.Analyze(device);
        
        Assert.NotEmpty(capabilities);
        Assert.Contains(capabilities, c => c.Type == AndroidCapabilityType.AdbRoot);
    }
    
    [Fact]
    public void Analyze_OemUnlockEnabled_ReturnsBootloaderUnlock()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        device.AddProperty("ro.oem_unlock_supported", "1");
        
        var capabilities = AndroidCapabilityAnalyzer.Analyze(device);
        
        Assert.NotEmpty(capabilities);
        Assert.Contains(capabilities, c => c.Type == AndroidCapabilityType.BootloaderUnlock);
    }
    
    [Fact]
    public void Analyze_QualcommDevice_ReturnsEdlCapability()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        device.AddProperty("ro.hardware", "qcom");
        device.AddProperty("ro.boot.hardware", "qcom");
        
        var capabilities = AndroidCapabilityAnalyzer.Analyze(device);
        
        Assert.NotEmpty(capabilities);
        Assert.Contains(capabilities, c => c.Type == AndroidCapabilityType.QualcommEdl);
    }
}

public class AndroidCapabilityPlannerTests
{
    [Fact]
    public void Plan_EmptyCapabilities_ReturnsNoOperations()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        var capabilities = new List<AndroidCapability>();
        
        var plan = AndroidCapabilityPlanner.Plan(device, capabilities);
        
        Assert.Empty(plan.Operations);
    }
    
    [Fact]
    public void Plan_WithAdbRoot_ReturnsAdbOperations()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        var capabilities = new List<AndroidCapability>
        {
            new AndroidCapability(AndroidCapabilityType.AdbRoot, "ADB root access", true)
        };
        
        var plan = AndroidCapabilityPlanner.Plan(device, capabilities);
        
        Assert.NotEmpty(plan.Operations);
        Assert.Contains(plan.Operations, o => o.Name.Contains("ADB"));
    }
}

public class AndroidFileServiceTests
{
    private readonly Mock<IAdbManager> _mockAdb = new();
    private readonly AppLogger _logger = new(new Workspace());
    private readonly AndroidFileService _service;
    
    public AndroidFileServiceTests()
    {
        _service = new AndroidFileService(_mockAdb.Object, _logger);
    }
    
    [Fact]
    public void Name_ReturnsAndroidFileService()
    {
        Assert.Equal("AndroidFileService", _service.Name);
    }
    
    [Fact]
    public async Task PullFileAsync_CallsAdbPull()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        _mockAdb.Setup(a => a.PullFileAsync(device.SerialNumber, "/path/to/file", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var result = await _service.PullFileAsync(device, "/path/to/file", "/local/dest", CancellationToken.None);
        
        Assert.True(result);
        _mockAdb.Verify(a => a.PullFileAsync(device.SerialNumber, "/path/to/file", "/local/dest", It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task PushFileAsync_CallsAdbPush()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        _mockAdb.Setup(a => a.PushFileAsync(device.SerialNumber, "/local/file", "/remote/dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var result = await _service.PushFileAsync(device, "/local/file", "/remote/dest", CancellationToken.None);
        
        Assert.True(result);
        _mockAdb.Verify(a => a.PushFileAsync(device.SerialNumber, "/local/file", "/remote/dest", It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class AndroidPackageServiceTests
{
    private readonly Mock<IAdbManager> _mockAdb = new();
    private readonly AppLogger _logger = new(new Workspace());
    private readonly AndroidPackageService _service;
    
    public AndroidPackageServiceTests()
    {
        _service = new AndroidPackageService(_mockAdb.Object, _logger);
    }
    
    [Fact]
    public void Name_ReturnsAndroidPackageService()
    {
        Assert.Equal("AndroidPackageService", _service.Name);
    }
    
    [Fact]
    public async Task InstallPackageAsync_CallsAdbInstall()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        _mockAdb.Setup(a => a.InstallPackageAsync(device.SerialNumber, "/path/to/app.apk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var result = await _service.InstallPackageAsync(device, "/path/to/app.apk", CancellationToken.None);
        
        Assert.True(result);
        _mockAdb.Verify(a => a.InstallPackageAsync(device.SerialNumber, "/path/to/app.apk", It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task UninstallPackageAsync_CallsAdbUninstall()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        _mockAdb.Setup(a => a.UninstallPackageAsync(device.SerialNumber, "com.example.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var result = await _service.UninstallPackageAsync(device, "com.example.app", CancellationToken.None);
        
        Assert.True(result);
        _mockAdb.Verify(a => a.UninstallPackageAsync(device.SerialNumber, "com.example.app", It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task ListPackagesAsync_CallsAdbShell()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        _mockAdb.Setup(a => a.ExecuteCommandAsync(device.SerialNumber, "pm list packages", It.IsAny<CancellationToken>()))
            .ReturnsAsync("package:com.example.app1\npackage:com.example.app2");
        
        var packages = await _service.ListPackagesAsync(device, CancellationToken.None);
        
        Assert.NotEmpty(packages);
        Assert.Contains("com.example.app1", packages);
        Assert.Contains("com.example.app2", packages);
    }
}

public class AndroidDiagnosticServiceTests
{
    private readonly Mock<IAdbManager> _mockAdb = new();
    private readonly AppLogger _logger = new(new Workspace());
    private readonly AndroidDiagnosticService _service;
    
    public AndroidDiagnosticServiceTests()
    {
        _service = new AndroidDiagnosticService(_mockAdb.Object, _logger);
    }
    
    [Fact]
    public void Name_ReturnsAndroidDiagnosticService()
    {
        Assert.Equal("AndroidDiagnosticService", _service.Name);
    }
    
    [Fact]
    public async Task GetBootloaderInfoAsync_ReturnsInfo()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        _mockAdb.Setup(a => a.ExecuteCommandAsync(device.SerialNumber, "getprop ro.bootloader", It.IsAny<CancellationToken>()))
            .ReturnsAsync("unknown");
        
        var info = await _service.GetBootloaderInfoAsync(device, CancellationToken.None);
        
        Assert.NotNull(info);
    }
    
    [Fact]
    public async Task GetPartitionInfoAsync_ReturnsPartitions()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        _mockAdb.Setup(a => a.ExecuteCommandAsync(device.SerialNumber, "ls /dev/block/by-name", It.IsAny<CancellationToken>()))
            .ReturnsAsync("boot\nrecovery\nsystem");
        
        var partitions = await _service.GetPartitionInfoAsync(device, CancellationToken.None);
        
        Assert.NotEmpty(partitions);
    }
}

public class AndroidSnapshotServiceTests
{
    private readonly Mock<IAdbManager> _mockAdb = new();
    private readonly AppLogger _logger = new(new Workspace());
    private readonly Workspace _workspace = new();
    private readonly AndroidSnapshotService _service;
    
    public AndroidSnapshotServiceTests()
    {
        _service = new AndroidSnapshotService(_mockAdb.Object, _logger, _workspace);
    }
    
    [Fact]
    public void Name_ReturnsAndroidSnapshotService()
    {
        Assert.Equal("AndroidSnapshotService", _service.Name);
    }
    
    [Fact]
    public async Task CaptureSnapshotAsync_CreatesSnapshot()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        device.AddProperty("ro.build.version.sdk", "30");
        device.AddProperty("ro.product.cpu.abi", "arm64-v8a");
        
        _mockAdb.Setup(a => a.ExecuteCommandAsync(device.SerialNumber, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test output");
        
        var snapshot = await _service.CaptureSnapshotAsync(device, CancellationToken.None);
        
        Assert.NotNull(snapshot);
        Assert.Equal(device.SerialNumber, snapshot.DeviceSerial);
        Assert.NotEmpty(snapshot.Properties);
    }
}

public class AndroidBackupServiceTests
{
    private readonly Mock<IAdbManager> _mockAdb = new();
    private readonly AppLogger _logger = new(new Workspace());
    private readonly Workspace _workspace = new();
    private readonly AndroidBackupService _service;
    
    public AndroidBackupServiceTests()
    {
        _service = new AndroidBackupService(_mockAdb.Object, _logger, _workspace);
    }
    
    [Fact]
    public void Name_ReturnsAndroidBackupService()
    {
        Assert.Equal("AndroidBackupService", _service.Name);
    }
    
    [Fact]
    public async Task BackupPartitionAsync_CallsAdbBackup()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        _mockAdb.Setup(a => a.BackupPartitionAsync(device.SerialNumber, "boot", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var result = await _service.BackupPartitionAsync(device, "boot", "/backup/boot.img", CancellationToken.None);
        
        Assert.True(result);
    }
    
    [Fact]
    public async Task RestorePartitionAsync_CallsAdbRestore()
    {
        var device = new AndroidDevice("serial", "model", "manufacturer");
        _mockAdb.Setup(a => a.RestorePartitionAsync(device.SerialNumber, "boot", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var result = await _service.RestorePartitionAsync(device, "boot", "/backup/boot.img", CancellationToken.None);
        
        Assert.True(result);
    }
}
