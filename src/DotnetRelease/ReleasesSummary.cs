using DotnetRelease.Graph;

namespace DotnetRelease;

/// <summary>
/// Provides a high-level summary of all .NET releases with support status.
/// This replaces the functionality of the legacy releases-index.json file.
/// Data is fetched lazily on first query and cached for subsequent queries.
/// </summary>
public class ReleasesSummary
{
    private readonly ReleaseNotesGraph _graph;
    private MajorReleaseVersionIndex? _index;

    public ReleasesSummary(ReleaseNotesGraph graph)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    /// <summary>
    /// Ensures the major release index is loaded, fetching it if necessary.
    /// </summary>
    private async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_index is not null) return;

        _index = await _graph.GetMajorReleaseIndexAsync(cancellationToken);
        if (_index is null)
            throw new InvalidOperationException("Failed to load major release index");
    }

    /// <summary>
    /// Gets all .NET versions in the index (latest first).
    /// </summary>
    public async Task<IEnumerable<ReleaseSummary>> GetAllVersionsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _index!.Embedded?.Releases?.Select(r => new ReleaseSummary(r))
            ?? Enumerable.Empty<ReleaseSummary>();
    }

    /// <summary>
    /// Gets all currently supported .NET versions.
    /// </summary>
    public async Task<IEnumerable<ReleaseSummary>> GetSupportedVersionsAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetAllVersionsAsync(cancellationToken);
        return all.Where(r => r.IsSupported);
    }

    /// <summary>
    /// Gets versions filtered by support phase (Preview, Active, Maintenance, Eol).
    /// </summary>
    public async Task<IEnumerable<ReleaseSummary>> GetVersionsByPhaseAsync(SupportPhase phase, CancellationToken cancellationToken = default)
    {
        var all = await GetAllVersionsAsync(cancellationToken);
        return all.Where(r => r.Phase == phase);
    }

    /// <summary>
    /// Gets versions filtered by release type (LTS or STS).
    /// </summary>
    public async Task<IEnumerable<ReleaseSummary>> GetVersionsByTypeAsync(ReleaseType type, CancellationToken cancellationToken = default)
    {
        var all = await GetAllVersionsAsync(cancellationToken);
        return all.Where(r => r.ReleaseType == type);
    }

    /// <summary>
    /// Gets the latest .NET version (regardless of support status).
    /// </summary>
    public async Task<ReleaseSummary?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetAllVersionsAsync(cancellationToken);
        return all.FirstOrDefault();
    }

    /// <summary>
    /// Gets the latest LTS (Long-Term Support) version.
    /// </summary>
    public async Task<ReleaseSummary?> GetLatestLtsAsync(CancellationToken cancellationToken = default)
    {
        var ltsVersions = await GetVersionsByTypeAsync(ReleaseType.LTS, cancellationToken);
        return ltsVersions.FirstOrDefault();
    }

    /// <summary>
    /// Gets the latest STS (Standard-Term Support) version.
    /// </summary>
    public async Task<ReleaseSummary?> GetLatestStsAsync(CancellationToken cancellationToken = default)
    {
        var stsVersions = await GetVersionsByTypeAsync(ReleaseType.STS, cancellationToken);
        return stsVersions.FirstOrDefault();
    }

    /// <summary>
    /// Gets the latest supported version (LTS or STS).
    /// </summary>
    public async Task<ReleaseSummary?> GetLatestSupportedAsync(CancellationToken cancellationToken = default)
    {
        var supported = await GetSupportedVersionsAsync(cancellationToken);
        return supported.FirstOrDefault();
    }

    /// <summary>
    /// Gets a specific version by its version string (e.g., "8.0").
    /// Returns null if the version is not found.
    /// </summary>
    public async Task<ReleaseSummary?> GetVersionAsync(string version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(version);

        var all = await GetAllVersionsAsync(cancellationToken);
        return all.FirstOrDefault(r => r.Version == version);
    }

    /// <summary>
    /// Checks if a specific version is currently supported.
    /// </summary>
    public async Task<bool> IsSupportedAsync(string version, CancellationToken cancellationToken = default)
    {
        var versionInfo = await GetVersionAsync(version, cancellationToken);
        return versionInfo?.IsSupported ?? false;
    }

    /// <summary>
    /// Gets the End of Life date for a specific version.
    /// Returns null if the version is not found or has no EOL date.
    /// </summary>
    public async Task<DateTimeOffset?> GetEolDateAsync(string version, CancellationToken cancellationToken = default)
    {
        var versionInfo = await GetVersionAsync(version, cancellationToken);
        return versionInfo?.EolDate;
    }

    /// <summary>
    /// Creates a ReleaseNavigator for deep exploration of a specific version.
    /// </summary>
    public ReleaseNavigator GetNavigator(string version)
    {
        return new ReleaseNavigator(_graph, version);
    }
}
