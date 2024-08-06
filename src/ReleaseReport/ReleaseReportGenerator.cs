using DotnetRelease;
using EndOfLifeDate;

public class ReleaseReportGenerator
{
    public static async Task<ReportOverview> GetReportOverviewAsync(int majorVersion, string? baseUrl = null)
    {
        baseUrl ??= "https://raw.githubusercontent.com/dotnet/core/main/release-notes/";

        string version = $"{majorVersion}.0";
        bool preferWeb = baseUrl.StartsWith("https");
        HttpClient client= new();
        DateOnly threeMonthsDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3));
        string supportMatrixUrl, releaseUrl;
        SupportedOSMatrix? matrix = null;
        MajorReleaseOverview? majorRelease = null;
        ReportOverview report = new(DateTime.UtcNow, version, []);

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

        DateOnly initialRelease = majorRelease?.Releases.FirstOrDefault(r => r.ReleaseVersion.Equals($"{majorVersion}.0.0"))?.ReleaseDate ?? DateOnly.MaxValue;
        DateOnly eolDate = majorRelease?.EolDate ?? DateOnly.MaxValue;

        foreach (SupportFamily family in matrix?.Families ?? throw new Exception())
        {
            ReportFamily reportFamily = new(family.Name, []);
            report.Families.Add(reportFamily);

            foreach (SupportDistribution distro in family.Distributions)
            {
                IList<SupportCycle>? cycles;
                List<string> activeReleases = [];
                List<string> unsupportedActiveRelease = [];
                List<string> soonEolReleases = [];
                List<string> supportedUnActiveReleases = [];
                List<string> missingReleases = [];
                
                try
                {
                    cycles = await EndOfLifeDate.Product.GetProduct(client, distro.Id);
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
                    bool hasOverlappingLifecycle = initialRelease <= support.EolDate && cycle.ReleaseDate <= eolDate;

                    if (isActive)
                    {
                        activeReleases.Add(cycle.Cycle);
                    }

                    /*
                        Cases (EOLDate, dotnet):
                        1. (Active, Supported)
                        2. (Active, Unsupported)
                        3. (OverlappingLifecycle, Unlisted)
                        4. (EOL, Supported)
                        5. (Active - EolSoon, Supported)
                        // these are not covered
                        6. (Unlisted, Listed)
                        7. (EOL, Unsupported | Unlisted)
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
                    else if (hasOverlappingLifecycle && !isSupported && !isUnsupported)
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

                ReportDistribution reportDistribution = new(distro.Name, activeReleases, unsupportedActiveRelease, soonEolReleases, supportedUnActiveReleases, missingReleases);
                reportFamily.Distributions.Add(reportDistribution);
            }
        }

        return report;
    }
}
