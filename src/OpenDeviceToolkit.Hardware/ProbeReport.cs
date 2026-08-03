namespace OpenDeviceToolkit.Hardware;

public sealed record ProbeReport(
    double? TargetVoltage,
    IReadOnlyList<ProtocolHypothesis> Hypotheses,
    IReadOnlyList<DecodedByte> DecodedBytes,
    string Recommendation);

public sealed class ProbeReportBuilder
{
    public ProbeReport Build(double? voltage, IReadOnlyList<ProtocolHypothesis> hypotheses, IReadOnlyList<DecodedByte> decoded)
    {
        var top = hypotheses.OrderByDescending(x => x.Confidence).FirstOrDefault();
        var recommendation = top is null
            ? "No reliable protocol identified. Keep the probe passive and capture more activity."
            : $"Investigate {top.Protocol} first ({top.Confidence:P0} confidence). Do not drive target pins until voltage and interface levels are verified.";
        return new(voltage, hypotheses, decoded, recommendation);
    }
}
