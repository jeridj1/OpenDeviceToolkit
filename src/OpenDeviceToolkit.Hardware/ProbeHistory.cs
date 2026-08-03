namespace OpenDeviceToolkit.Hardware;

public sealed record ProbeObservation(DateTimeOffset Time, string Category, string Detail, double? Confidence = null);

public sealed class ProbeHistory
{
    private readonly List<ProbeObservation> _items = [];
    public IReadOnlyList<ProbeObservation> Items => _items;
    public void Add(string category, string detail, double? confidence = null)
        => _items.Add(new(DateTimeOffset.UtcNow, category, detail, confidence));
}
