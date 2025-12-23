using DotnetRelease.Security;
using DotnetRelease.Graph;

namespace DotnetRelease;

/// <summary>
/// Provides a summary of a .NET patch release.
/// This wraps data from PatchReleaseVersionIndexEntry.
/// For CVE details, follow the month or cve-json links.
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
    public SupportPhase? Phase => _entry.SupportPhase;

    /// <summary>
    /// Date when this patch was released (GA date)
    /// </summary>
    public DateTimeOffset? ReleaseDate => _entry.Date;

    /// <summary>
    /// True if this is a security update (has CVE fixes).
    /// For CVE IDs and details, follow the month or cve-json links.
    /// </summary>
    public bool IsSecurityUpdate => _entry.Security;

    /// <summary>
    /// HAL links for navigation to this patch's content
    /// </summary>
    public IReadOnlyDictionary<string, HalLink>? Links => _entry.Links;
}
