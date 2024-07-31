namespace DotnetRelease;

public record ReportOverview(DateTime Timestamp, string Version, IList<ReportFamily> Families);

public record ReportFamily(string Name, IList<ReportDistribution> Distributions);

public record ReportDistribution(string Name, IList<string> ActiveReleases, IList<string> ActiveReleasesUnsupported, IList<string> ActiveReleasesEOLSoon, IList<string>  NotActiveReleasesSupported, IList<string>  ReleasesMissing);
