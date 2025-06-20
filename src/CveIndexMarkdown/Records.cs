namespace CveIndexMarkdown;

record ReleaseHistory(string Year, List<ReleaseDays> ReleaseDays);

record ReleaseDays(DateOnly Date, int Month, int Day, List<Release> Releases)
{
    public string? CveJson { get; set; }
};

record Release(string Version, bool Security)
{
    public int[]? Severity { get; set; }
};
