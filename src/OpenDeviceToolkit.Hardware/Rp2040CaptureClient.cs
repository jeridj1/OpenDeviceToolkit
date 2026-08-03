namespace OpenDeviceToolkit.Hardware;

public sealed class Rp2040CaptureClient(Rp2040BridgeClient bridge)
{
    public LogicCapture Capture(int gpio, uint sampleRateHz, int words = 256)
    {
        if (gpio is < 0 or > 29) throw new ArgumentOutOfRangeException(nameof(gpio));
        if (sampleRateHz == 0) throw new ArgumentOutOfRangeException(nameof(sampleRateHz));
        if (words is < 1 or > 1024) throw new ArgumentOutOfRangeException(nameof(words));
        var response = bridge.Command($"CAPTURE {gpio} {sampleRateHz} {words}");
        var fields = response.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length < 4 || fields[0] != "CAPTURE") throw new InvalidDataException(response);
        var rate = uint.Parse(fields[1]);
        var count = int.Parse(fields[2]);
        var samples = new uint[count];
        if (fields.Length - 3 < count) throw new InvalidDataException("Incomplete capture returned by RP2040.");
        for (var i = 0; i < count; i++) samples[i] = Convert.ToUInt32(fields[i + 3], 16);
        return new LogicCapture(rate, samples);
    }
}
