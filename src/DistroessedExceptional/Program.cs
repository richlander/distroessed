using DotnetRelease;
using ExceptionalVersions = System.Collections.Generic.List<string>;
using ExceptionsPerVersion = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>;
using ExceptionsPerFamily = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>>>;
using FileHelpers;

if (args.Length is 0 || !int.TryParse(args[0], out int majorVersion))
{
    ReportInvalidArgs();
    return;
}

Console.WriteLine("Checking for problems in the supported OS matrix");

// Version strings
string version = $"{majorVersion}.0";

// Get path adaptor
string basePath = args.Length > 1 ? args[1] : Location.OfficialBaseUri;
using HttpClient client = new();
IAdaptivePath path = AdaptivePath.GetFromDefaultAdaptors(basePath, client);

ExceptionsPerFamily exceptions = new()
{
    ["Windows"] = new()
    {
        ["Windows"] = new()
        {
            ["6.0"] = ["10-1507-e-lts"],
            ["7.0"] = ["10-1507-e-lts"],
            ["8.0"] = ["10-1507-e-lts"]
        },
        ["Windows Server"] = new()
        {
            ["6.0"] = ["2012", "2012-R2"],
            ["7.0"] = ["2012", "2012-R2"],
            ["8.0"] = ["2012", "2012-R2"],
            ["9.0"] = ["2012", "2012-R2"]
        },
        ["Windows Server Core"] = new()
        {
            ["6.0"] = ["2012", "2012-R2"],
            ["7.0"] = ["2012", "2012-R2"],
            ["8.0"] = ["2012", "2012-R2"],
            ["9.0"] = ["2012", "2012-R2"]
        }
    },
    ["Linux"] = new()
    {
        ["CentOS Stream"] = new()
        {
            ["8.0"] = ["8"]
        },
        ["Debian"] = new()
        {
            ["6.0"] = ["11"],
            ["8.0"] = ["12"]
        },
        ["Red Hat Enterprise Linux"] = new()
        {
            ["8.0"] = ["7"]
        }
    }
};

// Acquire JSON data, locally or from the web
string supportJson = path.Combine(version, ReleaseNotes.SupportedOS);
using Stream supportStream = await path.GetStreamAsync(supportJson);
SupportedOSMatrix matrix = await ReleaseNotes.GetSupportedOSes(supportStream) ?? throw new();
string releasesJson = path.Combine(version, ReleaseNotes.Releases);
using Stream releasesJsonStream = await path.GetStreamAsync(releasesJson);
MajorReleaseOverview majorRelease = await ReleaseNotes.GetMajorRelease(releasesJsonStream) ?? throw new();

var report = await ReleaseReportGenerator.GetReportOverviewAsync(matrix, majorRelease);

var reportVersion = report.Version;
Console.WriteLine($"* .NET {reportVersion}");
if (majorRelease?.SupportPhase == SupportPhase.Eol)
{
    Console.WriteLine("** This version is EOL and therefore checks can not be performed as the documents should show the state as of EOL instead of today.");
    return;
}
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
