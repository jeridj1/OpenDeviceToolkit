namespace OpenDeviceToolkit.Hardware;

public sealed record ProtocolHypothesis(string Protocol, double Confidence, string Reason, IReadOnlyDictionary<string, string> Parameters);

public sealed class ProtocolDetector
{
    public IReadOnlyList<ProtocolHypothesis> Analyze(LogicCapture capture, int bit = 0)
    {
        var results = new List<ProtocolHypothesis>();
        var frequency = new LogicCaptureAnalyzer().EstimateFrequency(capture, bit);
        if (frequency is > 1000 and < 100_000_000)
            results.Add(new("clock-like digital signal", .35, $"Detected periodic activity near {frequency:0.##} Hz.", new Dictionary<string, string> { ["frequencyHz"] = frequency.Value.ToString("0.##") }));

        var bits = Unpack(capture, bit);
        var uart = new UartAnalyzer().RankCandidates(bits, capture.SampleRateHz);
        foreach (var candidate in uart.Take(3))
            results.Add(new("UART", Math.Clamp(candidate.Score, 0, 1), candidate.Evidence, new Dictionary<string, string> { ["baud"] = candidate.BaudRate.ToString(), ["dataBits"] = candidate.DataBits.ToString(), ["stopBits"] = candidate.StopBits.ToString(), ["inverted"] = candidate.Inverted.ToString() }));
        return results.OrderByDescending(x => x.Confidence).ToArray();
    }

    private static IReadOnlyList<bool> Unpack(LogicCapture capture, int bit)
    {
        var result = new List<bool>(capture.Words.Count * capture.BitsPerWord);
        foreach (var word in capture.Words)
            for (var b = 0; b < capture.BitsPerWord; b++) result.Add(((word >> bit) & 1u) != 0);
        return result;
    }
}
