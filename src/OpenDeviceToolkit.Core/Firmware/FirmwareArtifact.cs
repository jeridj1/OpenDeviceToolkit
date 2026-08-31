using System.Security.Cryptography;

namespace OpenDeviceToolkit.Core.Firmware;

/// <summary>
/// Outcome of validating a firmware artifact against expected metadata.
/// </summary>
public enum FirmwareValidationStatus
{
    /// <summary>Acquired but not yet validated.</summary>
    Unknown,
    /// <summary>Checksum, size, and model all match expectations.</summary>
    Verified,
    /// <summary>Computed SHA-256 does not match the expected checksum.</summary>
    HashMismatch,
    /// <summary>File size does not match the expected size.</summary>
    SizeMismatch,
    /// <summary>Artifact target model does not match the connected device model.</summary>
    ModelMismatch,
    /// <summary>The firmware file is missing on disk.</summary>
    Missing
}

/// <summary>
/// Expected properties a firmware artifact must satisfy before it may be used
/// in a write operation. Any field left null is not checked.
/// </summary>
public sealed record FirmwareArtifactSpec(
    string? ExpectedSha256 = null,
    long? ExpectedSize = null,
    string? ExpectedModel = null,
    string? DeviceModel = null)
{
    public static FirmwareArtifactSpec Empty => new();
}

/// <summary>
/// A firmware file acquired into the workspace along with its computed hash,
/// expected metadata, and validation result. Validation status is the
/// precondition the operation guard checks before any persistent write.
/// </summary>
public sealed record FirmwareArtifact(
    string FilePath,
    long Size,
    string Sha256,
    FirmwareArtifactSpec Spec,
    FirmwareValidationStatus ValidationStatus,
    string ValidationMessage);
