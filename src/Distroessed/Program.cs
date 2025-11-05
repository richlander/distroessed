using DotnetRelease;
using DotnetRelease.ReleaseInfo;
using DotnetRelease.Support;
using DotnetRelease.Summary;
using FileHelpers;

if (args.Length is 0 || !int.TryParse(args[0], out int majorVersion))
{
    ReportInvalidArgs();
    return;
}

string version = $"{majorVersion}.0";

// Get path adaptor
string basePath = args.Length > 1 ? args[1] : ReleaseNotes.OfficialBaseUri;
using HttpClient client = new();
IAdaptivePath path = AdaptivePath.GetFromDefaultAdaptors(basePath, client);

// Acquire JSON data, locally or from the web
string supportJson = path.Combine(version, ReleaseNotes.SupportedOS);
using Stream supportStream = await path.GetStreamAsync(supportJson);
SupportedOSMatrix matrix = await ReleaseNotes.GetSupportedOSes(supportStream) ?? throw new();
string releasesJson = path.Combine(version, ReleaseNotes.Releases);
using Stream releasesJsonStream = await path.GetStreamAsync(releasesJson);
MajorReleaseOverview majorRelease = await ReleaseNotes.GetMajorRelease(releasesJsonStream) ?? throw new();

var report = await ReleaseReportGenerator.GetReportOverviewAsync(matrix, majorRelease);
var reportJson = ReleaseReport.WriteReport(report);

Console.WriteLine(reportJson);


static void ReportInvalidArgs()
{
    Console.WriteLine("Invalid args.");
    Console.WriteLine("Expected: version [URL or Path, absolute or root location]");
}
