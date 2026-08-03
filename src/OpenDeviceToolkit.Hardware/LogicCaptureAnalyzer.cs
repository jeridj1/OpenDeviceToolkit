namespace OpenDeviceToolkit.Hardware;

public sealed record LogicCapture(uint SampleRateHz, IReadOnlyList<uint> Words, int BitsPerWord = 32);
public sealed record EdgeEvent(long SampleIndex, bool Rising);

public sealed class LogicCaptureAnalyzer
{
    public IReadOnlyList<EdgeEvent> FindEdges(LogicCapture capture, int bit = 0)
    {
        if (bit < 0 || bit >= capture.BitsPerWord) throw new ArgumentOutOfRangeException(nameof(bit));
        var edges = new List<EdgeEvent>();
        var previous = false;
        var initialized = false;
        long index = 0;
        foreach (var word in capture.Words)
            for (var b = 0; b < capture.BitsPerWord; b++, index++)
            {
                var state = ((word >> bit) & 1u) != 0;
                if (initialized && state != previous) edges.Add(new(index, state));
                previous = state;
                initialized = true;
            }
        return edges;
    }

    public double? EstimateFrequency(LogicCapture capture, int bit = 0)
    {
        var edges = FindEdges(capture, bit);
        if (edges.Count < 3 || capture.SampleRateHz == 0) return null;
        var periods = new List<long>();
        for (var i = 2; i < edges.Count; i += 2) periods.Add(edges[i].SampleIndex - edges[i - 2].SampleIndex);
        if (periods.Count == 0) return null;
        var average = periods.Average();
        return average > 0 ? capture.SampleRateHz / average : null;
    }
}
