namespace OpenDeviceToolkit.Hardware;

public sealed record UartCandidate(int BaudRate, int DataBits, int StopBits, bool Inverted, double Score, string Evidence);

public sealed class UartAnalyzer
{
    private static readonly int[] CommonBauds = { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 };

    public IReadOnlyList<UartCandidate> RankCandidates(IReadOnlyList<bool> samples, uint sampleRate)
    {
        var results = new List<UartCandidate>();
        if (sampleRate == 0 || samples.Count < 32) return results;
        foreach (var baud in CommonBauds)
        {
            if (sampleRate < (uint)baud * 2) continue;
            foreach (var inverted in new[] { false, true })
            {
                var score = ScoreCandidate(samples, sampleRate, baud, inverted);
                if (score > 0.20)
                    results.Add(new(baud, 8, 1, inverted, score, $"UART timing fit at {baud} baud; {(inverted ? "inverted" : "normal")} polarity."));
            }
        }
        return results.OrderByDescending(x => x.Score).ToArray();
    }

    private static double ScoreCandidate(IReadOnlyList<bool> samples, uint rate, int baud, bool inverted)
    {
        var samplesPerBit = (double)rate / baud;
        var transitions = new List<int>();
        bool? previous = null;
        for (var i = 0; i < samples.Count; i++)
        {
            var state = samples[i] ^ inverted;
            if (previous.HasValue && state != previous.Value) transitions.Add(i);
            previous = state;
        }
        if (transitions.Count < 4) return 0;
        var good = transitions.Count(t => Math.Abs((t / samplesPerBit) - Math.Round(t / samplesPerBit)) < 0.18);
        return Math.Min(1.0, (double)good / transitions.Count);
    }
}
