using OpenDeviceToolkit.Core;

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
        UsbDeviceInfo? deviceInfo = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if this source is available (e.g., network connectivity for online sources).
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
