namespace ReleaseReport;

public record Report(DateTime Timestamp, string Version, IList<ReportFamily> Families);

public record ReportFamily(string Name, IList<ReportDistribution> Distributions);

public record ReportDistribution(string Name, int ReleasesActive, int ReleasesMissing, int ReleasesUnsupportedActive, int ReleasesSupportNotActive, IList<string> ReleasesEOLSoon);
