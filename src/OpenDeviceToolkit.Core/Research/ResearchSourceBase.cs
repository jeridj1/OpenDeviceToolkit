using OpenDeviceToolkit.Core;

namespace OpenDeviceToolkit.Core.Research;

/// <summary>
/// Base class for research sources with common functionality.
/// </summary>
public abstract class ResearchSourceBase : IResearchSource
{
    public abstract string Name { get; }
    public virtual int Priority => 0;
    
    public abstract Task<IReadOnlyList<ResearchResult>> SearchAsync(
        string query,
        UsbDeviceInfo? deviceInfo = null,
        CancellationToken cancellationToken = default);
    
    public virtual Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => 
        Task.FromResult(true);
}
