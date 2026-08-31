using OpenDeviceToolkit.Android;
using OpenDeviceToolkit.Core.Firmware;

namespace OpenDeviceToolkit.Tests;

public sealed class RecoveryWorkflowTests
{
    // sha256("abc")
    private const string AbcSha256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    private static async Task<string> WriteFirmwareAsync()
    {
        var path = Path.GetTempFileName();
        await File.WriteAllBytesAsync(path, "abc"u8.ToArray());
        return path;
    }

    private sealed class FakeRecoveryOperation : IRecoveryOperation
    {
        public string Name { get; set; } = "flash boot";
        public OperationRisk Risk { get; set; } = OperationRisk.PersistentWrite;
        public bool ExecuteResult { get; set; } = true;
        public bool VerifyResult { get; set; } = true;
        public int ExecuteCalls;
        public int VerifyCalls;

        public Task<bool> ExecuteAsync(RecoveryRequest request, CancellationToken cancellationToken = default)
        {
            ExecuteCalls++;
            return Task.FromResult(ExecuteResult);
        }

        public Task<bool> VerifyAsync(RecoveryRequest request, CancellationToken cancellationToken = default)
        {
            VerifyCalls++;
            return Task.FromResult(VerifyResult);
        }
    }

    private static RecoveryRequest Request(string path, bool confirmed, string iface = "fastboot", string? expectedSha = AbcSha256) =>
        new("ABC123", "TestModel", iface, path,
            new FirmwareArtifactSpec(ExpectedSha256: expectedSha, ExpectedSize: 3, ExpectedModel: "TestModel", DeviceModel: "TestModel"),
            confirmed);

    [Fact]
    public async Task NoSupportedInterface_StopsBeforeWrite()
    {
        var path = await WriteFirmwareAsync();
        try
        {
            var result = await new RecoveryWorkflow(new FirmwareArtifactService()).RunAsync(Request(path, confirmed: true, iface: "none"));

            Assert.False(result.FirmwareValidated);
            Assert.False(result.OperationExecuted);
            Assert.Contains(result.Phases, p => p.Name == "Detect interface" && !p.Success);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task InvalidFirmware_StopsBeforeWrite()
    {
        var path = await WriteFirmwareAsync();
        try
        {
            var result = await new RecoveryWorkflow(new FirmwareArtifactService()).RunAsync(Request(path, confirmed: true, expectedSha: "deadbeef" + new string('0', 56)));

            Assert.False(result.FirmwareValidated);
            Assert.False(result.OperationExecuted);
            Assert.DoesNotContain(result.Phases, p => p.Name == "Guarded operation");
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ValidFirmware_Unconfirmed_BlocksWrite()
    {
        var path = await WriteFirmwareAsync();
        var op = new FakeRecoveryOperation();
        try
        {
            var result = await new RecoveryWorkflow(new FirmwareArtifactService(), op).RunAsync(Request(path, confirmed: false));

            Assert.True(result.FirmwareValidated);
            Assert.False(result.OperationExecuted);
            Assert.Equal(0, op.ExecuteCalls);
            Assert.Contains("blocked by guard", result.Summary, StringComparison.OrdinalIgnoreCase);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ValidFirmware_Confirmed_ExecutesAndVerifies()
    {
        var path = await WriteFirmwareAsync();
        var op = new FakeRecoveryOperation();
        try
        {
            var result = await new RecoveryWorkflow(new FirmwareArtifactService(), op).RunAsync(Request(path, confirmed: true));

            Assert.True(result.FirmwareValidated);
            Assert.True(result.OperationExecuted);
            Assert.True(result.Verified);
            Assert.Equal(1, op.ExecuteCalls);
            Assert.Equal(1, op.VerifyCalls);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task NoOperationBound_IsNoOpAndVerified()
    {
        var path = await WriteFirmwareAsync();
        try
        {
            var result = await new RecoveryWorkflow(new FirmwareArtifactService()).RunAsync(Request(path, confirmed: true));

            Assert.True(result.FirmwareValidated);
            Assert.False(result.OperationExecuted);
            Assert.True(result.Verified);
        }
        finally { File.Delete(path); }
    }
}
