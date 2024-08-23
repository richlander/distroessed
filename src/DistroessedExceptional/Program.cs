using DotnetRelease;
using ExceptionalVersions = System.Collections.Generic.List<string>;
using ExceptionsPerVersion = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>;
using ExceptionsPerFamily = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>>>;

if (args.Length is 0 || !int.TryParse(args[0], out int majorVersion))
{
    ReportInvalidArgs();
    return;
}

ExceptionsPerFamily exceptions = new()
{
    ["Windows"] = new()
    {
        ["Windows"] = new()
        {
            ["6.0"] = [ "10-1507-e-lts" ],
            ["7.0"] = [ "10-1507-e-lts" ],
            ["8.0"] = [ "10-1507-e-lts" ]
        },
        ["Windows Server"] = new()
        {
            ["6.0"] = [ "2012", "2012-R2" ],
            ["7.0"] = [ "2012", "2012-R2" ],
            ["8.0"] = [ "2012", "2012-R2" ],
            ["9.0"] = [ "2012", "2012-R2" ]
        },
        ["Windows Server Core"] = new()
        {
            ["6.0"] = [ "2012", "2012-R2" ],
            ["7.0"] = [ "2012", "2012-R2" ],
            ["8.0"] = [ "2012", "2012-R2" ],
            ["9.0"] = [ "2012", "2012-R2" ]
        }
    },
    ["Linux"] = new()
    {
        ["Alpine"] = new()
        {
            ["7.0"] = [ "3.14" ]
        },
        ["CentOS Stream"] = new()
        {
            ["8.0"] = [ "8" ]
        },
        ["Red Hat Enterprise Linux"] = new()
        {
            ["8.0"] = [ "7" ]
        },
        ["SUSE Enterprise Linux"] = new()
        {
            ["7.0"] = [ "15.6" ]
        }
    }
};

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

var reportVersion = report.Version;
Console.WriteLine($"* .NET {reportVersion}");
foreach (var family in report.Families)
{
    exceptions.TryGetValue(family.Name, out var familyExceptions);

    foreach (var distribution in family.Distributions)
    {
        ExceptionsPerVersion? versionExceptions = null;
        ExceptionalVersions? distroExceptions = null;
        var distroName = distribution.Name;
        familyExceptions?.TryGetValue(distroName, out versionExceptions);
        versionExceptions?.TryGetValue(reportVersion, out distroExceptions);

        foreach (var dVersion in distribution.ActiveReleasesEOLSoon)
        {
            PrintIfNotExpected(distroName, dVersion, distroExceptions, "EOL Soon");
        }
        foreach (var dVersion in distribution.NotActiveReleasesSupported)
        {
            PrintIfNotExpected(distroName, dVersion, distroExceptions, "EOL but still supported");
        }
        foreach (var dVersion in distribution.ReleasesMissing)
        {
            PrintIfNotExpected(distroName, dVersion, distroExceptions, "Currently missing");
        }
    }
}


static void ReportInvalidArgs()
{
    Console.WriteLine("Invalid args.");
    Console.WriteLine("Expected: version [URL or Path, absolute or root location]");
}

void PrintIfNotExpected(string distroName, string distroVersion, ExceptionalVersions? distroExceptions, string text)
{
    if (distroExceptions?.Contains(distroVersion) == true) return;

    Console.WriteLine($"** {distroName} {distroVersion}: {text}");
}