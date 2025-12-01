using DotnetRelease.ReleaseInfo;

namespace DotnetRelease.Summary;

public record PatchReleaseSummary
(
    string MajorVersion,
    string PatchVersion,
    DateOnly ReleaseDate,
    bool Security,
    IList<Cve> CveList,
    IList<ReleaseComponent> Components
)
{
    public string? ReleaseJsonPath { get; set; }

    /// <summary>
    /// The relative path to the patch directory (e.g., "10.0/10.0.0" or "10.0/preview/preview1")
    /// </summary>
    public string? PatchDirPath { get; set; }
}

public record ReleaseComponent
(
    string Name,
    string Version,
    string Label
);
