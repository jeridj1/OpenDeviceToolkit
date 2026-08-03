namespace OpenDeviceToolkit.Hardware;

public static class Rp2040CaptureParser
{
    public static LogicCapture Parse(string line)
    {
        var fields = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length < 3 || fields[0] != "CAPTURE") throw new FormatException("Invalid RP2040 capture response.");
        if (!uint.TryParse(fields[1], out var rate) || !uint.TryParse(fields[2], out var count)) throw new FormatException("Invalid capture header.");
        if (fields.Length - 3 != count) throw new FormatException($"Capture declares {count} words but returned {fields.Length - 3}.");
        var words = new uint[count];
        for (var i = 0; i < count; i++)
            if (!uint.TryParse(fields[i + 3], System.Globalization.NumberStyles.HexNumber, null, out words[i])) throw new FormatException("Invalid capture word.");
        return new LogicCapture(rate, words);
    }
}
