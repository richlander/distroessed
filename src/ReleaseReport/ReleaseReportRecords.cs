namespace ReleaseReport;

public record Report(DateTime Timestamp, string Version, IList<ReportFamily> Families);

public record ReportFamily(string Name, IList<ReportDistribution> Distributions);

public record ReportDistribution(string Name, IList<string> ActiveReleases, IList<string> ActiveReleasesUnsupported, IList<string> ActiveReleasesEOLSoon, IList<string>  NotActiveReleasesSupported, IList<string>  ReleasesMissing);
