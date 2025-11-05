namespace DotnetRelease.Summary;

public record MajorReleaseSummary
(
    string MajorVersion,
    string MajorVersionLabel,
    ReleaseType ReleaseType,
    SupportPhase SupportPhase,
    DateTimeOffset GaDate,
    DateTimeOffset EolDate,
    IList<SdkBand> SdkBands,
    IList<PatchReleaseSummary> PatchReleases
);
