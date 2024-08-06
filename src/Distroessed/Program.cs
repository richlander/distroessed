using DotnetRelease;

if (args.Length is 0 || !int.TryParse(args[0], out int majorVersion))
{
    ReportInvalidArgs();
    return;
}

string defaultBaseUrl = "https://raw.githubusercontent.com/dotnet/core/main/release-notes/";
string baseUrl = args.Length > 1 ? args[1] : defaultBaseUrl;

string version = $"{majorVersion}.0";
bool preferWeb = baseUrl.StartsWith("https");
HttpClient client= new();
string supportMatrixUrl, releaseUrl;
SupportedOSMatrix? matrix = null;
MajorReleaseOverview? majorRelease = null;

if (preferWeb)
{
    supportMatrixUrl = $"{baseUrl}/{version}/supported-os.json";
    releaseUrl = $"{baseUrl}/{version}/releases.json";
}
else
{
    supportMatrixUrl = Path.Combine(baseUrl, version,"supported-os.json");
    releaseUrl = Path.Combine(baseUrl, version,"releases.json");   
}

if (preferWeb)
{
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
