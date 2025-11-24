namespace DotnetRelease.Summary;

public record MajorReleaseSummary
(
    string MajorVersion,
    string MajorVersionLabel,
    Lifecycle Lifecycle,
    IList<SdkBand> SdkBands,
    IList<PatchReleaseSummary> PatchReleases
)
{
    // Convenience accessors for backwards compatibility
    public ReleaseType ReleaseType => Lifecycle.ReleaseType ?? ReleaseType.STS;
    public SupportPhase SupportPhase => Lifecycle.Phase;
    public DateTimeOffset GaDate => Lifecycle.ReleaseDate;
    public DateTimeOffset EolDate => Lifecycle.EolDate;
};
