using DotnetRelease.Graph;

namespace DotnetRelease;

/// <summary>
/// Provides deep navigation into a specific .NET major version, including patches, CVEs, and SDK information.
/// Data is fetched lazily as needed and cached for subsequent queries.
/// </summary>
public class ReleaseNavigator
{
    private readonly ReleaseNotesGraph _graph;
    private readonly string _version;
    private PatchReleaseVersionIndex? _patchIndex;
    private ReleaseManifest? _manifest;

    public ReleaseNavigator(ReleaseNotesGraph graph, string version)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _version = version ?? throw new ArgumentNullException(nameof(version));
    }

    /// <summary>
    /// The major version being navigated (e.g., "8.0", "9.0")
    /// </summary>
    public string Version => _version;

    /// <summary>
    /// Ensures the patch index is loaded, fetching it if necessary.
    /// </summary>
    private async Task<PatchReleaseVersionIndex> EnsurePatchIndexAsync(CancellationToken cancellationToken = default)
    {
        if (_patchIndex is not null) return _patchIndex;

        _patchIndex = await _graph.GetPatchReleaseIndexAsync(_version, cancellationToken);
        if (_patchIndex is null)
            throw new InvalidOperationException($"Failed to load patch index for version {_version}");

        return _patchIndex;
    }

    /// <summary>
    /// Ensures the manifest is loaded, fetching it if necessary.
    /// </summary>
    private async Task<ReleaseManifest> EnsureManifestAsync(CancellationToken cancellationToken = default)
    {
        if (_manifest is not null) return _manifest;

        _manifest = await _graph.GetManifestAsync(_version, cancellationToken);
        if (_manifest is null)
            throw new InvalidOperationException($"Failed to load manifest for version {_version}");

        return _manifest;
    }

    /// <summary>
    /// Gets the raw patch index document.
    /// </summary>
    public async Task<PatchReleaseVersionIndex> GetPatchIndexAsync(CancellationToken cancellationToken = default)
    {
        return await EnsurePatchIndexAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the raw manifest document.
    /// </summary>
    public async Task<ReleaseManifest> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        return await EnsureManifestAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all patch releases for this version (latest first).
    /// </summary>
    public async Task<IEnumerable<PatchSummary>> GetAllPatchesAsync(CancellationToken cancellationToken = default)
    {
        var index = await EnsurePatchIndexAsync(cancellationToken);
        return index.Embedded?.Releases?.Select(r => new PatchSummary(r))
            ?? Enumerable.Empty<PatchSummary>();
    }

    /// <summary>
    /// Gets the latest patch release for this version.
    /// </summary>
    public async Task<PatchSummary?> GetLatestPatchAsync(CancellationToken cancellationToken = default)
    {
        var patches = await GetAllPatchesAsync(cancellationToken);
        return patches.FirstOrDefault();
    }

    /// <summary>
    /// Gets a specific patch release by version string (e.g., "8.0.1").
    /// </summary>
    public async Task<PatchSummary?> GetPatchAsync(string patchVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(patchVersion);

        var patches = await GetAllPatchesAsync(cancellationToken);
        return patches.FirstOrDefault(p => p.Version == patchVersion);
    }

    /// <summary>
    /// Gets all patches that include security fixes (CVEs).
    /// </summary>
    public async Task<IEnumerable<PatchSummary>> GetSecurityPatchesAsync(CancellationToken cancellationToken = default)
    {
        var patches = await GetAllPatchesAsync(cancellationToken);
        return patches.Where(p => p.HasCves);
    }

    /// <summary>
    /// Gets all patches that have CVE records (alias for GetSecurityPatchesAsync).
    /// </summary>
    public async Task<IEnumerable<PatchSummary>> GetPatchesWithCvesAsync(CancellationToken cancellationToken = default)
    {
        return await GetSecurityPatchesAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if this version has any security updates available.
    /// </summary>
    public async Task<bool> HasSecurityUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var securityPatches = await GetSecurityPatchesAsync(cancellationToken);
        return securityPatches.Any();
    }

    /// <summary>
    /// Gets the version string of the latest patch (e.g., "8.0.17").
    /// </summary>
    public async Task<string?> GetLatestPatchVersionAsync(CancellationToken cancellationToken = default)
    {
        var latest = await GetLatestPatchAsync(cancellationToken);
        return latest?.Version;
    }

    /// <summary>
    /// Gets the release date of the latest patch.
    /// </summary>
    public async Task<DateTimeOffset?> GetLatestPatchDateAsync(CancellationToken cancellationToken = default)
    {
        var latest = await GetLatestPatchAsync(cancellationToken);
        return latest?.ReleaseDate;
    }
}
