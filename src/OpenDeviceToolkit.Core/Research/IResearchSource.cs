namespace OpenDeviceToolkit.Core.Research;

/// <summary>
/// Interface for research data sources (online search, local databases, etc.).
/// </summary>
public interface IResearchSource
{
    /// <summary>
    /// Gets the name of this research source.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the priority of this source (higher = searched first).
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Searches for information related to the query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="deviceInfo">Optional device information for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of research results.</returns>
    Task<IReadOnlyList<ResearchResult>> SearchAsync(
        string query,
        Usb.UsbDeviceInfo? deviceInfo = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if this source is available (e.g., network connectivity for online sources).
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for research sources with common functionality.
/// </summary>
public abstract class ResearchSourceBase : IResearchSource
{
    public abstract string Name { get; }
    public virtual int Priority => 0;
    
    public abstract Task<IReadOnlyList<ResearchResult>> SearchAsync(
        string query,
        Usb.UsbDeviceInfo? deviceInfo = null,
        CancellationToken cancellationToken = default);
    
    public virtual Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => 
        Task.FromResult(true);
}
