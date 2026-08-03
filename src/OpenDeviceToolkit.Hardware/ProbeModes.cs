namespace OpenDeviceToolkit.Hardware;

public enum ProbeMode { HighImpedanceSniffer, LogicAnalyzer, UartBridge, SwdDebugger, CmsisDap, SpiBridge, I2cBridge, GpioBridge, Programmer }
public sealed record ProbeModeProfile(ProbeMode Mode, IReadOnlyList<string> RequiredCapabilities, IReadOnlyList<string> Inputs, IReadOnlyList<string> Outputs, bool CanDriveTarget);

public static class ProbeModeCatalog
{
    public static IReadOnlyList<ProbeModeProfile> All { get; } =
    [
        new(ProbeMode.HighImpedanceSniffer, ["GPIO input", "timestamping"], ["target signal"], ["samples"], false),
        new(ProbeMode.LogicAnalyzer, ["GPIO input", "fast sampling", "timestamping"], ["target signals"], ["sample stream"], false),
        new(ProbeMode.UartBridge, ["GPIO input", "GPIO output", "timed serial"], ["RX/TX", "baud", "framing"], ["serial data"], true),
        new(ProbeMode.SwdDebugger, ["SWDIO", "SWCLK", "target voltage sense"], ["SWD signals"], ["debug transactions"], true),
        new(ProbeMode.CmsisDap, ["debug transport", "USB bridge"], ["debug target"], ["DAP packets"], true),
        new(ProbeMode.SpiBridge, ["GPIO", "timed output", "input sampling"], ["clock", "MOSI", "MISO", "CS"], ["SPI transactions"], true),
        new(ProbeMode.I2cBridge, ["open-drain GPIO", "pull-up sensing"], ["SDA", "SCL"], ["I2C transactions"], true),
        new(ProbeMode.GpioBridge, ["GPIO input/output"], ["pin map"], ["digital I/O"], true),
        new(ProbeMode.Programmer, ["target-specific transport", "verified artifact"], ["part", "artifact", "target voltage"], ["programming result"], true)
    ];
}

public sealed record ProbeSessionRequest(string Description, ProbeMode? RequestedMode = null, string? PartHint = null);

public sealed class ProbeModeSelector
{
    public ProbeModeProfile Select(ProbeSessionRequest request)
    {
        if (request.RequestedMode is { } requested) return ProbeModeCatalog.All.First(x => x.Mode == requested);
        var text = request.Description.ToLowerInvariant();
        if (text.Contains("logic analyzer") || text.Contains("capture") || text.Contains("sniff")) return ProbeModeCatalog.All.First(x => x.Mode == ProbeMode.LogicAnalyzer);
        if (text.Contains("uart") || text.Contains("serial")) return ProbeModeCatalog.All.First(x => x.Mode == ProbeMode.UartBridge);
        if (text.Contains("swd") || text.Contains("debug") || text.Contains("stm32")) return ProbeModeCatalog.All.First(x => x.Mode == ProbeMode.SwdDebugger);
        if (text.Contains("spi")) return ProbeModeCatalog.All.First(x => x.Mode == ProbeMode.SpiBridge);
        if (text.Contains("i2c")) return ProbeModeCatalog.All.First(x => x.Mode == ProbeMode.I2cBridge);
        return ProbeModeCatalog.All.First(x => x.Mode == ProbeMode.HighImpedanceSniffer);
    }
}
