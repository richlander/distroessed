using DotnetRelease;
using DotnetSupport;
using EndOfLifeDate;
using ReleaseReport;

if (args.Length is 0 || !int.TryParse(args[0], out int ver))
{
    ReportInvalidArgs();
    return;
}

string version = $"{ver}.0";
string baseDefaultURL = "https://raw.githubusercontent.com/dotnet/core/os-support/release-notes/";
string baseUrl = args.Length > 1 ? args[1] : baseDefaultURL;
bool preferWeb = baseUrl.StartsWith("https");
string supportMatrixUrl, releaseUrl;
SupportMatrix? matrix = null;
ReleaseOverview? release = null;
List<ReportFamily> reportFamilies = [];
Report report = new(DateTime.UtcNow, version, reportFamilies);

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

HttpClient client= new();
DateOnly threeMonthsDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3));

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

DateOnly initialRelease = release?.Releases.FirstOrDefault(r => r.ReleaseVersion.Equals($"{ver}.0.0"))?.ReleaseDate ?? DateOnly.MaxValue;
foreach (SupportFamily family in matrix?.Families ?? throw new Exception())
{
    List<ReportDistribution> reportDistributions = [];
    ReportFamily reportFamily = new(family.Name,reportDistributions);
    reportFamilies.Add(reportFamily);

    foreach (SupportDistribution distro in family.Distributions)
    {
        IList<SupportCycle>? cycles = null;
        List<SupportCycle> activeReleases = [];
        List<SupportCycle> missingCycles = [];
        List<SupportCycle> unsupportedActiveRelease = [];
        List<SupportCycle> soonEolReleases = [];
        List<SupportCycle> supportedEolReleases = [];
        
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
            Console.WriteLine("  No data found at endoflife.date");
            Console.WriteLine();
            continue;
        }

        foreach (SupportCycle cycle in cycles)
        {
            SupportInfo support = cycle.GetSupportInfo();
            bool distroCycleSupported = distro.SupportedVersions.Contains(cycle.Cycle);
            bool distroCycleUnsupported = distro.UnsupportedVersions?.Contains(cycle.Cycle) ?? false;
            bool isActive = support.IsActive;

            if (isActive)
            {
                activeReleases.Add(cycle);
            }
            else
            {
                if (distroCycleSupported)
                {
                    supportedEolReleases.Add(cycle);
                }
                else
                {
                    missingCycles.Add(cycle);
                }

                continue;
            }

            if (!distroCycleSupported)
            {
                unsupportedActiveRelease.Add(cycle);
            }

            if (support.EolDate > DateOnly.MinValue &&
                threeMonthsDate > support.EolDate)
            {
                soonEolReleases.Add(cycle);
            }
        }

        ReportDistribution reportDistribution = new(distro.Name, activeReleases, missingCycles, unsupportedActiveRelease, supportedEolReleases, soonEolReleases);
        Console.WriteLine($"  Releases active : {activeReleases}");
        Console.WriteLine($"  Missing releases: {missingCycles.Count}");
        Console.WriteLine($"  Unsupported active releases: {unsupportedActiveRelease.Count}");
        Console.WriteLine($"  Releases EOL soon: {soonEolReleases.Count}");
        Console.WriteLine($"  Supported inactive releases: {supportedEolReleases.Count}");

        PrintMessageAboutCycles(missingCycles.Count > 0, missingCycles, "Releases that had active support but were never supported:", 2);
        PrintMessageAboutCycles(unsupportedActiveRelease.Count > 0, unsupportedActiveRelease, "Releases that are active but not supported:", 2);
        PrintMessageAboutCycles(soonEolReleases.Count > 0, soonEolReleases, "Releases that are EOL within 2 months:", 2);
        PrintMessageAboutCycles(supportedEolReleases.Count > 0, supportedEolReleases, "Releases that are EOL but supported:", 2);

        Console.WriteLine();
    }
}

void ReportInvalidArgs()
{
    Console.WriteLine("Invalid args.");
    Console.WriteLine("Expected: version [URL or Path, absolute or root location]");
}

void PrintMessageAboutCycles(bool condition, IEnumerable<SupportCycle> cycles, string message, int indent = 0)
{
    if (!condition)
    {
        return;
    }

    WriteIndent(indent);
    Console.WriteLine(message);

    foreach (SupportCycle cycle in cycles)
    {
        WriteIndent(indent);
        Console.WriteLine(cycle.Cycle);
    }
}

void WriteIndent(int indent)
{
    if (indent is 0)
    {
        return;
    }

    for (int i = 0; i < indent; i++)
    {
        Console.Write(' ');
    }
}
