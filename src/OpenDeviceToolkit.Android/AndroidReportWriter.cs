using System.Text;
using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Android;

public sealed class AndroidReportWriter
{
    public string Write(Workspace workspace, AndroidDevice device)
    {
        workspace.EnsureDirectories();
        var safeSerial = string.Concat(device.Serial.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));
        var filename = $"android-{safeSerial}-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        var path = Path.Combine(workspace.Reports, filename);

        var sb = new StringBuilder();
        sb.AppendLine("Open Device Toolkit - Android Device Report");
        sb.AppendLine($"Generated: {DateTime.Now:O}");
        sb.AppendLine();
        sb.AppendLine("Summary");
        sb.AppendLine("-------");
        Add(sb, "Serial", device.Serial);
        Add(sb, "Manufacturer", device.Manufacturer);
        Add(sb, "Model", device.Model);
        Add(sb, "Android", device.AndroidVersion);
        Add(sb, "Security patch", device.SecurityPatch);
        Add(sb, "Build ID", device.BuildId);
        Add(sb, "Software version", device.SoftwareVersion);
        Add(sb, "Platform", device.Platform);
        Add(sb, "Hardware", device.Hardware);
        Add(sb, "Verified boot state", device.BootState);
        Add(sb, "Flash locked", device.FlashLocked);
        Add(sb, "Slot", device.Slot);
        sb.AppendLine();
        sb.AppendLine("Raw properties");
        sb.AppendLine("--------------");
        foreach (var property in device.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            Add(sb, property.Key, property.Value);

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private static void Add(StringBuilder sb, string key, string value) => sb.AppendLine($"{key}: {value}");
}
