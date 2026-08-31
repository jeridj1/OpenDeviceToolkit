using System.Security.Cryptography;

namespace OpenDeviceToolkit.Core.Firmware;

/// <summary>
/// Device-independent firmware acquisition, hashing, and validation. This service
/// makes the planner's "Verified target artifact and checksum" precondition
/// checkable without any device or hardware attached.
/// </summary>
public sealed class FirmwareArtifactService
{
    /// <summary>
    /// Acquires a firmware file from disk, computes its size and SHA-256, and returns
    /// an artifact whose validation status is <see cref="FirmwareValidationStatus.Unknown"/>
    /// until <see cref="ValidateAsync"/> is called.
    /// </summary>
    public async Task<FirmwareArtifact> AcquireAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("A firmware file path is required.", nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Firmware file was not found.", filePath);

        var info = new FileInfo(filePath);
        var sha256 = await ComputeHashAsync(filePath, cancellationToken);
        return new FirmwareArtifact(
            filePath,
            info.Length,
            sha256,
            FirmwareArtifactSpec.Empty,
            FirmwareValidationStatus.Unknown,
            "Artifact acquired; not yet validated.");
    }

    /// <summary>
    /// Computes the lowercase hex SHA-256 of a file.
    /// </summary>
    public static async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Firmware file was not found.", filePath);

        using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Recomputes the hash and validates the artifact against <paramref name="spec"/>:
    /// checksum (SHA-256), expected size, and model match. Any mismatch sets the
    /// corresponding <see cref="FirmwareValidationStatus"/> so the caller can reject
    /// the artifact before any write operation.
    /// </summary>
    public async Task<FirmwareArtifact> ValidateAsync(FirmwareArtifact artifact, FirmwareArtifactSpec spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (!File.Exists(artifact.FilePath))
        {
            return artifact with
            {
                Spec = spec,
                ValidationStatus = FirmwareValidationStatus.Missing,
                ValidationMessage = "Firmware file no longer exists."
            };
        }

        var sha256 = await ComputeHashAsync(artifact.FilePath, cancellationToken);
        var size = new FileInfo(artifact.FilePath).Length;
        var messages = new List<string>();
        var status = FirmwareValidationStatus.Verified;

        if (!string.IsNullOrWhiteSpace(spec.ExpectedSha256) &&
            !string.Equals(sha256, spec.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            status = FirmwareValidationStatus.HashMismatch;
            messages.Add($"SHA-256 mismatch: expected {spec.ExpectedSha256}, computed {sha256}.");
        }

        if (spec.ExpectedSize is long expectedSize && size != expectedSize)
        {
            if (status == FirmwareValidationStatus.Verified)
                status = FirmwareValidationStatus.SizeMismatch;
            messages.Add($"Size mismatch: expected {expectedSize} bytes, file is {size} bytes.");
        }

        if (!string.IsNullOrWhiteSpace(spec.ExpectedModel) &&
            !string.IsNullOrWhiteSpace(spec.DeviceModel) &&
            !string.Equals(spec.ExpectedModel, spec.DeviceModel, StringComparison.OrdinalIgnoreCase))
        {
            if (status == FirmwareValidationStatus.Verified)
                status = FirmwareValidationStatus.ModelMismatch;
            messages.Add($"Model mismatch: artifact targets '{spec.ExpectedModel}', device is '{spec.DeviceModel}'.");
        }

        return artifact with
        {
            Size = size,
            Sha256 = sha256,
            Spec = spec,
            ValidationStatus = status,
            ValidationMessage = messages.Count == 0
                ? "Verified: checksum, size, and model match."
                : string.Join(" ", messages)
        };
    }
}
