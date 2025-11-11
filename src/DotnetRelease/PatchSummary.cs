using DotnetRelease.Security;
using DotnetRelease.Graph;

namespace DotnetRelease;

/// <summary>
/// Provides a summary of a .NET patch release with CVE information.
/// This wraps data from PatchReleaseVersionIndexEntry.
/// </summary>
public class PatchSummary
{
    private readonly PatchReleaseVersionIndexEntry _entry;

    public PatchSummary(PatchReleaseVersionIndexEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entry = entry;
    }

    /// <summary>
    /// Patch version identifier (e.g., "8.0.1", "9.0.2")
    /// </summary>
    public string Version => _entry.Version;

    /// <summary>
    /// Current lifecycle phase
    /// </summary>
    public SupportPhase? Phase => _entry.Lifecycle?.Phase;

    /// <summary>
    /// Date when this patch was released
    /// </summary>
    public DateTimeOffset? ReleaseDate => _entry.Lifecycle?.ReleaseDate;

    /// <summary>
    /// CVE IDs associated with this patch
    /// </summary>
    public IReadOnlyList<string>? CveRecords => _entry.CveRecords;

    /// <summary>
    /// True if this patch includes CVE fixes
    /// </summary>
    public bool HasCves => CveRecords?.Count > 0;

    /// <summary>
    /// Number of CVEs addressed in this patch
    /// </summary>
    public int CveCount => CveRecords?.Count ?? 0;

    /// <summary>
    /// True if this is a security update (has CVE fixes)
    /// </summary>
    public bool IsSecurityUpdate => HasCves;

    /// <summary>
    /// HAL links for navigation to this patch's content
    /// </summary>
    public IReadOnlyDictionary<string, HalLink>? Links => _entry.Links;
}
