using DotnetRelease.ReleaseInfo;
using DotnetRelease.Support;
using DotnetRelease.Summary;
using EndOfLifeDate;

public class ReleaseReportGenerator
{
    public static async Task<ReportOverview> GetReportOverviewAsync(SupportedOSMatrix? matrix, MajorReleaseOverview? majorRelease)
    {
        string version = matrix?.ChannelVersion
                         ?? majorRelease?.ChannelVersion
                         ?? "0.0";
        HttpClient client = new();
        DateOnly threeMonthsDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3));
        DateOnly initialRelease = majorRelease?.Releases.FirstOrDefault(r => r.ReleaseVersion.Equals($"{version}.0"))?.ReleaseDate ?? DateOnly.MaxValue;
        DateOnly eolDate = majorRelease?.EolDate ?? DateOnly.MaxValue;
        ReportOverview report = new(DateTime.UtcNow, version, []);

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
                List<string> unsupportedUnActiveReleases = [];
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
                        5. (EOL, Unsupported)
                        6. (Active - EolSoon, Supported)
                        // these are not covered
                        7. (Listed, Unlisted)
                        8. (EOL, Unlisted)
                        9. (Unlisted, Listed)
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
                    // Case 5
                    else if (!isActive && isUnsupported)
                    {
                        unsupportedUnActiveReleases.Add(cycle.Cycle);
                    }

                    // Case 6
                    if (isActive && support.EolDate < threeMonthsDate)
                    {
                        soonEolReleases.Add(cycle.Cycle);
                    }
                }

                ReportDistribution reportDistribution = new(distro.Name, activeReleases, unsupportedActiveRelease, soonEolReleases, supportedUnActiveReleases, unsupportedUnActiveReleases, missingReleases);
                reportFamily.Distributions.Add(reportDistribution);
            }
        }

        return report;
    }
}
