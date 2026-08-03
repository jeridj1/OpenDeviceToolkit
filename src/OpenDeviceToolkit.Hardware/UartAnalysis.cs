namespace OpenDeviceToolkit.Hardware;

public sealed record UartCandidate(int BaudRate, int DataBits, int StopBits, bool Inverted, double Score, string Evidence);

/// <summary>Scores common UART configurations from sampled digital data.</summary>
public sealed class UartAnalyzer
{
    private static readonly int[] CommonBauds = [1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600];

    public IReadOnlyList<UartCandidate> RankCandidates(IReadOnlyList<bool> samples, double sampleRateHz)
    {
        if (samples.Count < 32 || sampleRateHz <= 0) return [];
        var result = new List<UartCandidate>();
        foreach (var baud in CommonBauds)
        {
            var bitSamples = sampleRateHz / baud;
            if (bitSamples < 2) continue;
            foreach (var inverted in new[] { false, true })
                result.Add(new(baud, 8, 1, inverted, Score(samples, bitSamples, inverted), $"Sample timing fit at {baud} baud."));
        }
        return result.OrderByDescending(x => x.Score).Take(6).ToArray();
    }

    private static double Score(IReadOnlyList<bool> samples, double bitSamples, bool inverted)
    {
        var transitions = new List<int>();
        for (var i = 1; i < samples.Count; i++) if (samples[i] != samples[i - 1]) transitions.Add(i);
        if (transitions.Count < 2) return 0;
        var good = transitions.Count(t => Math.Abs((t % bitSamples) / bitSamples) < .12 || Math.Abs(1 - (t % bitSamples) / bitSamples) < .12);
        var transitionScore = (double)good / transitions.Count;
        var idle = inverted ? samples[0] : !samples[0];
        var idleScore = samples.Take(Math.Min(samples.Count, 16)).Count(x => x == idle) / 16.0;
        return transitionScore * .8 + idleScore * .2;
    }
}
