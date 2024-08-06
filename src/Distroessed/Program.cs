using DotnetRelease;

if (args.Length is 0 || !int.TryParse(args[0], out int majorVersion))
{
    ReportInvalidArgs();
    return;
}

string? baseUrl = args.Length > 1 ? args[1] : null;

string version = $"{majorVersion}.0";
string supportMatrixUrl = ReleaseNotes.GetUri(ReleaseNotes.SupportedOS, version, baseUrl);
string releaseUrl = ReleaseNotes.GetUri(ReleaseNotes.Releases, version, baseUrl);
bool preferWeb = supportMatrixUrl.StartsWith("https");
SupportedOSMatrix? matrix = null;
MajorReleaseOverview? majorRelease = null;

if (preferWeb)
{
    HttpClient client = new();
    matrix = await ReleaseNotes.GetSupportedOSes(client, supportMatrixUrl);
    majorRelease = await ReleaseNotes.GetMajorRelease(client, releaseUrl);
}   
else
{
    matrix = await ReleaseNotes.GetSupportedOSes(File.OpenRead(supportMatrixUrl));
    majorRelease = await ReleaseNotes.GetMajorRelease(File.OpenRead(releaseUrl));
}

var report = await ReleaseReportGenerator.GetReportOverviewAsync(matrix, majorRelease);
var reportJson = ReleaseReport.WriteReport(report);

Console.WriteLine(reportJson);


static void ReportInvalidArgs()
{
    Console.WriteLine("Invalid args.");
    Console.WriteLine("Expected: version [URL or Path, absolute or root location]");
}
