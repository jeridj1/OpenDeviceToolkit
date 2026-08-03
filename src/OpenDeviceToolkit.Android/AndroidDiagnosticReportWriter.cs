using System.Text;
using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Android;

public sealed class AndroidDiagnosticReportWriter
{
    public string Write(Workspace workspace, AndroidDevice device, IReadOnlyList<AndroidDiagnosticResult> diagnostics)
    {
        workspace.EnsureDirectories();
        var safeSerial = string.Concat(device.Serial.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));
        var filename = $"android-diagnostics-{safeSerial}-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        var path = Path.Combine(workspace.Reports, filename);

        var sb = new StringBuilder();
        sb.AppendLine("Open Device Toolkit - Android Read-Only Diagnostic Report");
        sb.AppendLine($"Generated: {DateTime.Now:O}");
        sb.AppendLine();
        sb.AppendLine("Device");
        sb.AppendLine("------");
        Add(sb, "Serial", device.Serial);
        Add(sb, "Manufacturer", device.Manufacturer);
        Add(sb, "Model", device.Model);
        Add(sb, "Android", device.AndroidVersion);
        Add(sb, "Build ID", device.BuildId);
        Add(sb, "Software version", device.SoftwareVersion);
        Add(sb, "Platform", device.Platform);
        Add(sb, "Hardware", device.Hardware);
        sb.AppendLine();
        sb.AppendLine("Read-only probes");
        sb.AppendLine("----------------");

        foreach (var diagnostic in diagnostics)
        {
            sb.AppendLine();
            sb.AppendLine($"[{(diagnostic.Success ? "PASS" : "FAIL")}] {diagnostic.Name}");
            sb.AppendLine($"Duration: {diagnostic.Duration.TotalMilliseconds:F0} ms");
            sb.AppendLine(diagnostic.Output);
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private static void Add(StringBuilder sb, string key, string value) => sb.AppendLine($"{key}: {value}");
}
