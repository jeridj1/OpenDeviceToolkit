namespace OpenDeviceToolkit.Android;

/// <summary>
/// Structured result of A/B (seamless update) slot analysis. Unknown fields are empty
/// strings; findings always state what was observed versus what remains unknown.
/// </summary>
public sealed record AndroidSlotReport(
    bool IsSeamlessUpdateCapable,
    string ActiveSlot,
    string FallbackSlot,
    string SlotSuffix,
    bool VirtualAbEnabled,
    IReadOnlyList<string> Findings,
    IReadOnlyList<string> Evidence)
{
    public const string Unknown = "Unknown";

    public static readonly AndroidSlotReport Empty = new(
        IsSeamlessUpdateCapable: false,
        ActiveSlot: string.Empty,
        FallbackSlot: string.Empty,
        SlotSuffix: string.Empty,
        VirtualAbEnabled: false,
        Findings: new[] { "No slot evidence was provided; slot state is Unknown." },
        Evidence: Array.Empty<string>());
}

/// <summary>
/// Converts observed read-only evidence (device properties and the /dev/block/by-name
/// listing) into an evidence-backed A/B slot report. This analyzer never runs a
/// state-changing command and reports explicit Unknown whenever the evidence is
/// insufficient, per the project safety constraints.
/// </summary>
public sealed class AndroidSlotAnalyzer
{
    public const string SlotSuffixProperty = "ro.boot.slot_suffix";
    public const string VirtualAbProperty = "ro.virtual_ab.enabled";
    public const string ByNameListingCommand = "ls /dev/block/by-name";

    /// <summary>Pure parser; fully unit-testable without a device.</summary>
    public static AndroidSlotReport Analyze(IReadOnlyDictionary<string, string> properties, string? byNameListing)
    {
        if (properties is null)
            throw new ArgumentNullException(nameof(properties));

        var evidence = new List<string>();
        var findings = new List<string>();

        var suffix = Get(properties, SlotSuffixProperty).Trim();
        if (suffix.Length > 0)
        {
            evidence.Add(SlotSuffixProperty + "=" + suffix);
        }

        var hasSlotA = false;
        var hasSlotB = false;
        if (!string.IsNullOrWhiteSpace(byNameListing))
        {
            var separators = new[] { ' ', '	', '', '
' };
            foreach (var rawToken in byNameListing.Split(separators, StringSplitOptions.RemoveEmptyEntries))
            {
                var token = rawToken;
                var arrow = token.IndexOf("->", StringComparison.Ordinal);
                if (arrow >= 0)
                {
                    token = token[..arrow];
                }

                token = token.Trim('/');
                if (token.EndsWith("_a", StringComparison.Ordinal))
                {
                    hasSlotA = true;
                }
                else if (token.EndsWith("_b", StringComparison.Ordinal))
                {
                    hasSlotB = true;
                }
            }

            if (hasSlotA)
            {
                evidence.Add("Slot-suffixed partitions ending in _a observed in /dev/block/by-name.");
            }

            if (hasSlotB)
            {
                evidence.Add("Slot-suffixed partitions ending in _b observed in /dev/block/by-name.");
            }
        }

        var activeSlot = string.Empty;
        if (suffix.Length == 0)
        {
            findings.Add("No slot suffix property observed; the active slot is Unknown.");
        }
        else
        {
            var normalized = suffix.StartsWith("_", StringComparison.Ordinal) ? suffix : "_" + suffix;
            var slot = normalized.TrimStart('_').ToLowerInvariant();
            if (slot is "a" or "b")
            {
                activeSlot = slot;
                findings.Add("Observed slot suffix '" + suffix + "' identifies the active slot as '" + activeSlot + "'.");
            }
            else
            {
                findings.Add("Observed slot suffix '" + suffix + "' is not a recognized A/B suffix; the active slot is Unknown.");
            }
        }

        var capable = activeSlot.Length > 0 || (hasSlotA && hasSlotB);
        if (capable)
        {
            var basis = activeSlot.Length > 0
                ? "slot suffix evidence (" + SlotSuffixProperty + ")"
                : "observed slot-suffixed partitions for both slots";
            findings.Add("Device is A/B (seamless update) capable based on " + basis + ".");
        }
        else
        {
            findings.Add("No A/B evidence observed (no valid slot suffix and no paired slot-suffixed partitions); seamless update capability is Unknown.");
        }

        var fallbackSlot = string.Empty;
        if (activeSlot.Length == 0)
        {
            if (hasSlotA && hasSlotB)
            {
                findings.Add("Both slot partition sets are present, so a second slot likely exists, but the active slot is Unknown; the fallback slot is Unknown.");
            }
        }
        else
        {
            var candidate = activeSlot == "a" ? "b" : "a";
            var candidateObserved = candidate == "a" ? hasSlotA : hasSlotB;
            if (candidateObserved)
            {
                fallbackSlot = candidate;
                findings.Add("Partitions for slot '" + fallbackSlot + "' are present; it is the fallback (inactive) slot.");
            }
            else
            {
                findings.Add("No partitions observed for the opposite slot; the fallback slot is Unknown.");
            }
        }

        var virtualAbRaw = Get(properties, VirtualAbProperty).Trim();
        var virtualAbEnabled = virtualAbRaw is "1" or "true";
        if (virtualAbRaw.Length > 0)
        {
            evidence.Add(VirtualAbProperty + "=" + virtualAbRaw);
            if (virtualAbEnabled)
            {
                findings.Add("Virtual A/B is reported as enabled.");
            }
            else
            {
                findings.Add("Virtual A/B is reported as not enabled.");
            }
        }

        return new AndroidSlotReport(capable, activeSlot, fallbackSlot, suffix, virtualAbEnabled, findings, evidence);
    }

    /// <summary>
    /// Collects slot evidence from a connected device using read-only adb shell commands
    /// and returns the structured analysis. Never performs a state-changing operation.
    /// </summary>
    public static async Task<AndroidSlotReport> AnalyzeDeviceAsync(
        AdbManager adb,
        string serial,
        CancellationToken cancellationToken = default)
    {
        if (adb is null)
        {
            throw new ArgumentNullException(nameof(adb));
        }

        if (string.IsNullOrWhiteSpace(serial))
        {
            throw new ArgumentException("A device serial is required.", nameof(serial));
        }

        var suffixResult = await adb.RunShellAsync(serial, "getprop " + SlotSuffixProperty, cancellationToken);
        var virtualAbResult = await adb.RunShellAsync(serial, "getprop " + VirtualAbProperty, cancellationToken);
        var listingResult = await adb.RunShellAsync(serial, ByNameListingCommand, cancellationToken);

        var properties = new Dictionary<string, string>
        {
            [SlotSuffixProperty] = suffixResult.Success ? suffixResult.StandardOutput.Trim() : string.Empty,
            [VirtualAbProperty] = virtualAbResult.Success ? virtualAbResult.StandardOutput.Trim() : string.Empty,
        };

        var listing = listingResult.Success ? listingResult.StandardOutput : null;
        var report = Analyze(properties, listing);

        if (!suffixResult.Success || !listingResult.Success)
        {
            var notes = new List<string>(report.Findings)
            {
                "One or more read-only evidence commands failed; the corresponding conclusions are Unknown."
            };
            report = report with { Findings = notes };
        }

        return report;
    }

    private static string Get(IReadOnlyDictionary<string, string> properties, string key) =>
        properties.TryGetValue(key, out var value) ? value : string.Empty;
}