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
}

public record ReleaseComponent
(
    string Name,
    string Version,
    string Label
);
