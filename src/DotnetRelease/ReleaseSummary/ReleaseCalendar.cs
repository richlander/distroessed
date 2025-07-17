namespace DotnetRelease;

public record ReleaseHistory(Dictionary<string, ReleaseYear> Years);

public record ReleaseYear(string Year, Dictionary<string, ReleaseMonth> Months);

public record ReleaseMonth(string Month, Dictionary<string, ReleaseDay> Days);

public record ReleaseDay(DateOnly Date, string Month, string Day, List<PatchReleaseSummary> Releases)
{
    public string? CveJson { get; set; }
};
