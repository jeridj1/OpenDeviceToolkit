namespace OpenDeviceToolkit.Hardware;

public sealed record DecodedByte(int SampleIndex, byte Value, string Text);

public sealed class ProtocolDecoder
{
    public IReadOnlyList<DecodedByte> DecodeUart(IReadOnlyList<bool> samples, uint sampleRate, int baud, bool inverted = false, int dataBits = 8)
    {
        if (sampleRate == 0 || baud <= 0 || dataBits is < 5 or > 8) return [];
        var spb = (double)sampleRate / baud;
        var output = new List<DecodedByte>();
        for (var start = 1; start < samples.Count; start++)
        {
            if ((samples[start] ^ inverted) != false || (samples[start - 1] ^ inverted) == false) continue;
            var value = 0;
            var valid = true;
            for (var bit = 0; bit < dataBits; bit++)
            {
                var center = start + (int)Math.Round((1.5 + bit) * spb);
                if (center >= samples.Count) { valid = false; break; }
                if ((samples[center] ^ inverted)) value |= 1 << bit;
            }
            if (!valid) break;
            var stop = start + (int)Math.Round((1.5 + dataBits) * spb);
            if (stop >= samples.Count || (samples[stop] ^ inverted)) continue;
            output.Add(new(start, (byte)value, value is >= 32 and <= 126 ? ((char)value).ToString() : "."));
            start += Math.Max(1, (int)Math.Round((1 + dataBits + 1) * spb) - 1);
        }
        return output;
    }
}
