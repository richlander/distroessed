namespace CveIndex;

public record ReleaseCalendar(string Year, List<ReleaseDay> ReleaseDays);

public record ReleaseDay(DateOnly Date, int Month, int Day, List<Release> Releases)
{
    public string? CveJson { get; set; }
};

public record Release(string Version, bool Security)
{
    public IReadOnlyList<int>? Severity { get; set; }
};

public record SevCount(string Severity, int Count);
