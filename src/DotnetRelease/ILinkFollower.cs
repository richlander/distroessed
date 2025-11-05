namespace DotnetRelease;

/// <summary>
/// Abstraction for fetching HAL+JSON documents by URL.
/// Implementations can provide caching, mocking, or other strategies.
/// </summary>
public interface ILinkFollower
{
    /// <summary>
    /// Fetches a document of type T from the specified href URL.
    /// </summary>
    /// <typeparam name="T">The document type to fetch and deserialize</typeparam>
    /// <param name="href">The URL to fetch from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deserialized document, or null if not found</returns>
    Task<T?> FetchAsync<T>(string href, CancellationToken cancellationToken = default) where T : class;
}
