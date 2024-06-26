using DotnetRelease;
using DotnetSupport;
using EndOfLifeDate;
using ReleaseReport;

if (args.Length is 0 || !int.TryParse(args[0], out int majorVersion))
{
    ReportInvalidArgs();
    return;
}

string version = $"{majorVersion}.0";
string baseDefaultURL = "https://raw.githubusercontent.com/dotnet/core/os-support/release-notes/";
string baseUrl = args.Length > 1 ? args[1] : baseDefaultURL;
bool preferWeb = baseUrl.StartsWith("https");
HttpClient client= new();
DateOnly threeMonthsDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3));
string supportMatrixUrl, releaseUrl;
SupportMatrix? matrix = null;
ReleaseOverview? release = null;
Report report = new(DateTime.UtcNow, version, []);

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
    matrix = await SupportedOS.GetSupportMatrix(client, supportMatrixUrl);
    release = await Releases.GetDotnetRelease(client, releaseUrl);
}
else
{
    matrix = await SupportedOS.GetSupportMatrix(File.OpenRead(supportMatrixUrl));
    release = await Releases.GetDotnetRelease(File.OpenRead(supportMatrixUrl));
}

DateOnly initialRelease = release?.Releases.FirstOrDefault(r => r.ReleaseVersion.Equals($"{majorVersion}.0.0"))?.ReleaseDate ?? DateOnly.MaxValue;
foreach (SupportFamily family in matrix?.Families ?? throw new Exception())
{
    ReportFamily reportFamily = new(family.Name, []);
    report.Families.Add(reportFamily);

    foreach (SupportDistribution distro in family.Distributions)
    {
        IList<SupportCycle>? cycles = null;
        List<string> activeReleases = [];
        List<string> unsupportedActiveRelease = [];
        List<string> soonEolReleases = [];
        List<string> supportedUnActiveReleases = [];
        List<string> missingReleases = [];
        
        try
        {
            cycles = await Product.GetProduct(client, distro.Id);
            if (cycles is null)
            {
                continue;
            }
        }
        catch (HttpRequestException)
        {
            Console.WriteLine($"No data found at endoflife.date for: {distro.Id}");
            Console.WriteLine();
            continue;
        }

        foreach (SupportCycle cycle in cycles)
        {
            SupportInfo support = cycle.GetSupportInfo();
            // dotnet statements
            bool isSupported = distro.SupportedVersions.Contains(cycle.Cycle);
            bool isUnsupported = distro.UnsupportedVersions?.Contains(cycle.Cycle) ?? false;
            // EndofLife.Date statement
            bool isActive = support.IsActive;

            if (isActive)
            {
                activeReleases.Add(cycle.Cycle);
            }

            /*
                Cases (EOLDate, dotnet):
                1. (Active, Supported)
                2. (Active, Unsupported)
                3. (Active, Unlisted)
                4. (EOL, Supported)
                5. (Active - EolSoon, Supported)
                // these are not covered
                6. (Unlisted, Listed)
                7. (EOL, UnSupported | Unlisted)
                8. (Listed, Unlisted)
            */

            // Case 1
            if (isActive && isSupported)
            {
                // nothing to do
            }
            // Case 2
            else if (isActive && isUnsupported)
            {
                unsupportedActiveRelease.Add(cycle.Cycle);
            }
            // Case 3
            else if (isActive)
            {
                missingReleases.Add(cycle.Cycle);
            }
            // Case 4
            else if (!isActive && isSupported)
            {
                supportedUnActiveReleases.Add(cycle.Cycle);
            }            

            if (isActive && support.EolDate < threeMonthsDate)
            {
                soonEolReleases.Add(cycle.Cycle);
            }
        }

        ReportDistribution reportDistribution = new(distro.Name, activeReleases, unsupportedActiveRelease, soonEolReleases, supportedUnActiveReleases, missingReleases );
        reportFamily.Distributions.Add(reportDistribution);
    }
}

var reportJson = ReleaseReport.Release.WriteReport(report);

Console.WriteLine(reportJson);


static void ReportInvalidArgs()
{
    Console.WriteLine("Invalid args.");
    Console.WriteLine("Expected: version [URL or Path, absolute or root location]");
}

