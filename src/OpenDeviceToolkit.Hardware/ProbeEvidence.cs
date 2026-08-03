namespace OpenDeviceToolkit.Hardware;

public enum SignalKind { Unknown, Digital, UartLike, ClockLike, Analog }
public sealed record SignalObservation(string Pin, double? Voltage, SignalKind Kind, int? EstimatedBaud, string Evidence);
public sealed record ChipIdentification(string Family, string Part, uint? IdCode, double Confidence, string Evidence);

public sealed class ProbeEvidenceAnalyzer
{
    public SignalObservation ClassifyDigital(string pin, double voltage, double frequencyHz, double dutyCycle)
    {
        if (frequencyHz >= 300 && frequencyHz <= 5_000_000 && dutyCycle is > 0.2 and < 0.8)
            return new(pin, voltage, SignalKind.ClockLike, null, $"Periodic activity at approximately {frequencyHz:F0} Hz with {dutyCycle:P0} duty cycle.");
        if (frequencyHz >= 20 && frequencyHz <= 1_000_000)
            return new(pin, voltage, SignalKind.Digital, null, $"Digital activity detected at approximately {frequencyHz:F0} Hz.");
        return new(pin, voltage, SignalKind.Unknown, null, "Insufficient timing evidence for protocol classification.");
    }

    public ChipIdentification IdentifyStm32(uint idCode)
    {
        var deviceId = idCode & 0x0FFF;
        var (part, confidence) = deviceId switch
        {
            0x0410 => ("STM32F1 medium-density family", 0.95),
            0x0412 => ("STM32F2 family", 0.85),
            0x0413 => ("STM32F4 family", 0.90),
            0x0418 => ("STM32F1 connectivity-line family", 0.85),
            0x0420 => ("STM32F1 value-line family", 0.85),
            0x0431 => ("STM32F2/F4-era family", 0.70),
            0x0440 => ("STM32F0 family", 0.90),
            _ => ("Unknown STM32 family", 0.10)
        };
        return new("STMicroelectronics STM32", part, idCode, confidence, $"SWD/JTAG IDCODE device field 0x{deviceId:X3}.");
    }
}
