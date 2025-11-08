using DotnetRelease.Graph;

namespace DotnetRelease;

/// <summary>
/// Provides a high-level summary of a .NET major version with support status.
/// This wraps data from MajorReleaseVersionIndexEntry.
/// </summary>
public class ReleaseSummary
{
    private readonly MajorReleaseVersionIndexEntry _entry;

    public ReleaseSummary(MajorReleaseVersionIndexEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entry = entry;
    }

    /// <summary>
    /// Major version identifier (e.g., "8.0", "9.0")
    /// </summary>
    public string Version => _entry.Version;

    /// <summary>
    /// Release support model (LTS or STS)
    /// </summary>
    public ReleaseType? ReleaseType => _entry.Lifecycle?.ReleaseType;

    /// <summary>
    /// Current lifecycle phase (Preview, Active, Maintenance, Eol)
    /// </summary>
    public SupportPhase? Phase => _entry.Lifecycle?.Phase;

    /// <summary>
    /// Date when this version was released
    /// </summary>
    public DateTimeOffset? ReleaseDate => _entry.Lifecycle?.ReleaseDate;

    /// <summary>
    /// End of Life date when support ends
    /// </summary>
    public DateTimeOffset? EolDate => _entry.Lifecycle?.EolDate;

    /// <summary>
    /// Whether this release is currently supported
    /// </summary>
    public bool IsSupported => _entry.Lifecycle?.Supported ?? false;

    /// <summary>
    /// True if this is a Long-Term Support release
    /// </summary>
    public bool IsLts => ReleaseType == DotnetRelease.ReleaseType.LTS;

    /// <summary>
    /// True if this is a Standard-Term Support release
    /// </summary>
    public bool IsSts => ReleaseType == DotnetRelease.ReleaseType.STS;

    /// <summary>
    /// True if this release is in the Active support phase
    /// </summary>
    public bool IsActive => Phase == SupportPhase.Active;

    /// <summary>
    /// True if this release is in the Preview phase
    /// </summary>
    public bool IsPreview => Phase == SupportPhase.Preview;

    /// <summary>
    /// True if this release has reached End of Life
    /// </summary>
    public bool IsEol => Phase == SupportPhase.Eol;

    /// <summary>
    /// HAL links for navigation to this version's content
    /// </summary>
    public IReadOnlyDictionary<string, HalLink>? Links => _entry.Links;
}
