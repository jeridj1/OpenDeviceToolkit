using OpenDeviceToolkit.Core.Firmware;

namespace OpenDeviceToolkit.Tests;

public sealed class FirmwareArtifactServiceTests
{
    // SHA-256 of the 3-byte UTF-8 string "abc".
    private const string AbcSha256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    private static async Task<string> WriteTempAsync(byte[] contents)
    {
        var path = Path.GetTempFileName();
        await File.WriteAllBytesAsync(path, contents);
        return path;
    }

    [Fact]
    public async Task ComputeHashAsync_MatchesKnownVector()
    {
        var path = await WriteTempAsync("abc"u8.ToArray());
        try
        {
            var hash = await FirmwareArtifactService.ComputeHashAsync(path);
            Assert.Equal(AbcSha256, hash);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task AcquireAsync_SetsSizeAndHash()
    {
        var path = await WriteTempAsync("abc"u8.ToArray());
        try
        {
            var artifact = await new FirmwareArtifactService().AcquireAsync(path);
            Assert.Equal(3, artifact.Size);
            Assert.Equal(AbcSha256, artifact.Sha256);
            Assert.Equal(FirmwareValidationStatus.Unknown, artifact.ValidationStatus);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task AcquireAsync_ThrowsWhenFileMissing()
    {
        var missing = Path.Combine(Path.GetTempPath(), "odt-does-not-exist-" + Guid.NewGuid() + ".bin");
        await Assert.ThrowsAsync<FileNotFoundException>(() => new FirmwareArtifactService().AcquireAsync(missing));
    }

    [Fact]
    public async Task ValidateAsync_PassesWhenChecksumAndModelMatch()
    {
        var path = await WriteTempAsync("abc"u8.ToArray());
        try
        {
            var svc = new FirmwareArtifactService();
            var artifact = await svc.AcquireAsync(path);
            var validated = await svc.ValidateAsync(artifact, new FirmwareArtifactSpec(
                ExpectedSha256: AbcSha256,
                ExpectedSize: 3,
                ExpectedModel: "TestModel",
                DeviceModel: "testmodel"));

            Assert.Equal(FirmwareValidationStatus.Verified, validated.ValidationStatus);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ValidateAsync_RejectsMismatchedChecksum()
    {
        var path = await WriteTempAsync("abc"u8.ToArray());
        try
        {
            var svc = new FirmwareArtifactService();
            var artifact = await svc.AcquireAsync(path);
            var validated = await svc.ValidateAsync(artifact, new FirmwareArtifactSpec(
                ExpectedSha256: "deadbeef" + new string('0', 56)));

            Assert.Equal(FirmwareValidationStatus.HashMismatch, validated.ValidationStatus);
            Assert.Contains("SHA-256 mismatch", validated.ValidationMessage);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ValidateAsync_RejectsWrongModel()
    {
        var path = await WriteTempAsync("abc"u8.ToArray());
        try
        {
            var svc = new FirmwareArtifactService();
            var artifact = await svc.AcquireAsync(path);
            var validated = await svc.ValidateAsync(artifact, new FirmwareArtifactSpec(
                ExpectedModel: "ModelA",
                DeviceModel: "ModelB"));

            Assert.Equal(FirmwareValidationStatus.ModelMismatch, validated.ValidationStatus);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ValidateAsync_RejectsSizeMismatch()
    {
        var path = await WriteTempAsync("abc"u8.ToArray());
        try
        {
            var svc = new FirmwareArtifactService();
            var artifact = await svc.AcquireAsync(path);
            var validated = await svc.ValidateAsync(artifact, new FirmwareArtifactSpec(
                ExpectedSize: 999));

            Assert.Equal(FirmwareValidationStatus.SizeMismatch, validated.ValidationStatus);
        }
        finally { File.Delete(path); }
    }
}
