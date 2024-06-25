using DotnetRelease;
using DotnetSupport;
using EndOfLifeDate;

if (args.Length is 0 || !int.TryParse(args[0], out int version))
{
    ReportInvalidArgs();
    return;
}

string defaultSupportJson = $"https://raw.githubusercontent.com/dotnet/core/os-support/release-notes/{version}.0/supported-os.json";
string supportJson = args.Length > 1 ? args[1] : defaultSupportJson;
bool preferFilePath = false;

if (args.Length > 2 && int.TryParse(args[2], out int preferFileArg))
{
    preferFilePath = true;

    if (preferFileArg is 2)
    {
        supportJson = Path.Combine(args[1], $"{version}.0","supported-os.json");
    }
}


HttpClient client= new();
DateOnly threeMonthsDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3));

var version = 6;
var supportMatrixUrl = $@"K:\GitRepos\core\release-notes\{version}.0\supported-os.json";
var releaseUrl = $@"K:\GitRepos\core\release-notes\{version}.0\releases.json";
SupportMatrix? matrix = await SupportedOS.GetSupportMatrix(supportMatrixUrl);
ReleaseOverview? release = await Releases.GetDotnetReleaseLocal(releaseUrl);

DateOnly initialRelease = release?.Releases.FirstOrDefault(r => r.ReleaseVersion.Equals($"{version}.0.0"))?.ReleaseDate ?? DateOnly.MaxValue;
DateOnly eolDate = release?.EolDate ?? DateOnly.MaxValue;
bool productIsEol = eolDate < DateOnly.FromDateTime(DateTime.UtcNow);
foreach (SupportFamily family in matrix?.Families ?? throw new Exception())
{
    Console.WriteLine($"**{family.Name}**");

    foreach (SupportDistribution distro in family.Distributions)
    {
        IList<SupportCycle>? cycles = null;
        List<SupportCycle> missingCycles = [];
        List<SupportCycle> unsupportedActiveRelease = [];
        List<SupportCycle> soonEolReleases = [];
        List<SupportCycle> supportedEolReleases = [];
        int activeReleases = 0;

        Console.WriteLine($" {distro.Name}");
        
        try
        {
            cycles = await EndOfLifeDate.EndOfLifeDate.GetProduct(client, distro.Id);
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
            bool distroCycleListed = distro.SupportedVersions.Contains(cycle.Cycle);
            bool distroCycleUnlisted = distro.UnsupportedVersions?.Contains(cycle.Cycle) ?? false;
            bool isActive = support.Active;

            if (isActive)
            {
                activeReleases++;
            }

            if (!isActive || productIsEol)
            {
                if (distroCycleListed)
                {
                    supportedEolReleases.Add(cycle);
                }
                else if (!productIsEol && support.EolDate >= initialRelease && cycle.ReleaseDate <= eolDate && !distroCycleUnlisted)
                {
                    missingCycles.Add(cycle);
                }

                continue;
            }

            if (!distroCycleListed)
            {
                unsupportedActiveRelease.Add(cycle);
            }

            if (support.EolDate > DateOnly.MinValue &&
                threeMonthsDate > support.EolDate)
            {
                soonEolReleases.Add(cycle);
            }
        }

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
    Console.WriteLine("Expected: version [URL] [1 == URL is absolute file path; 2 == add $\"{version}.0\\supported-os.json\" to end");
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
