using System.Globalization;
using System.Net;
using DotnetRelease;
using DotnetRelease.ReleaseInfo;
using DotnetRelease.Summary;
using CveFromRelease = DotnetRelease.ReleaseInfo.Cve;
using CveFromCves = DotnetRelease.Cves.Cve;

namespace VersionIndex;

public class Summary
{
    public static async Task<List<MajorReleaseSummary>> GetReleaseSummariesAsync(string rootDir)
    {
        var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
        // Files to probe for to include as links

        // List of major version entries
        List<MajorReleaseSummary> majorEntries = [];

        // look at all the major version directories
        foreach (var majorVersionDir in Directory.EnumerateDirectories(rootDir).OrderDescending(numericStringComparer))
        {
            // The presence of a releases.json file indicates this is a major version directory
            var releasesJson = Path.Combine(majorVersionDir, "releases.json");
            if (!File.Exists(releasesJson))
            {
                continue;
            }

            var majorVersionDirName = Path.GetFileName(majorVersionDir);

            Console.WriteLine($"Processing major version directory: {majorVersionDir}");

            await using var stream = File.OpenRead(releasesJson);
            var major = await ReleaseNotes.GetMajorRelease(stream) ?? throw new InvalidOperationException($"Failed to read major release from {releasesJson}");

            var sdkBands = SdkBand.GetSdkBandsForMajorRelease(major);

            // List of patch version entries
            List<PatchReleaseSummary> patchEntries = [];

            foreach (var release in major.Releases)
            {

                if (release is null)
                {
                    Console.WriteLine($"No release information found; patch.Release is null.");
                    continue;
                }

                var patchJson = Path.Combine(majorVersionDir, release.ReleaseVersion, "release.json");
                bool patchExists = File.Exists(patchJson);

                var isSecurity = release.Security;
                List<ReleaseComponent> components = [];

                if (release.Runtime is not null)
                {
                    var runtimeVersion = release.Runtime.Version ?? throw new InvalidOperationException($"Runtime version is null in {patchJson}");
                    components.Add(new ReleaseComponent("Runtime", runtimeVersion, $".NET Runtime {runtimeVersion}"));
                }

                if (release.AspNetCoreRuntime is not null)
                {
                    var aspnetVersion = release.AspNetCoreRuntime.Version ?? throw new InvalidOperationException($"ASP.NET Core version is null in {patchJson}");
                    components.Add(new ReleaseComponent("ASP.NET Core", aspnetVersion, $".NET ASP.NET Core {aspnetVersion}"));
                }

                if (release.WindowsDesktop is not null)
                {
                    var windowsDesktopVersion = release.WindowsDesktop.Version ?? throw new InvalidOperationException($"Windows Desktop version is null in {patchJson}");
                    components.Add(new ReleaseComponent("Windows Desktop", windowsDesktopVersion, $".NET Windows Desktop {windowsDesktopVersion}"));
                }

                foreach (var sdk in release?.Sdks ?? [])
                {
                    var version = sdk.Version ?? throw new InvalidOperationException($"SDK version is null in {patchJson}");
                    var label = sdk.Version ?? $".NET SDK {version}";
                    components.Add(new ReleaseComponent("SDK", version, label));
                }

                if (release?.ReleaseVersion == null)
                {
                    throw new InvalidOperationException($"Release version is null in {patchJson}");
                }

                PatchReleaseSummary summary = new(major.ChannelVersion, release.ReleaseVersion, release.ReleaseDate, isSecurity, release.CveList, components)
                {
                    ReleaseJsonPath = patchExists ? Path.GetRelativePath(rootDir, patchJson) : null
                };
                patchEntries.Add(summary);
            }

            Console.WriteLine($"Patch releases found for .NET {majorVersionDirName}: {patchEntries.Count}");

            IList<PatchReleaseSummary> patchVersions = patchEntries.Count is 0 ? Array.Empty<PatchReleaseSummary>() : patchEntries;

            var gaRelease = major.Releases.Where(p => !p.ReleaseVersion.Contains("preview", StringComparison.OrdinalIgnoreCase)).LastOrDefault();
            DateOnly gaDate = gaRelease?.ReleaseDate ?? DateOnly.MinValue;

            MajorReleaseSummary majorSummary = new MajorReleaseSummary(
                major.ChannelVersion,
                $".NET " + major.ChannelVersion,
                major.ReleaseType,
                major.SupportPhase,
                new DateTimeOffset(gaDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                new DateTimeOffset(major.EolDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                sdkBands,
                patchVersions
            );
            majorEntries.Add(majorSummary);
        }

        return majorEntries;
    }

    public static ReleaseHistory GetReleaseCalendar(List<MajorReleaseSummary> majorReleases)
    {
        var years = new Dictionary<string, ReleaseYear>();
        foreach (var major in majorReleases)
        {
            foreach (var patch in major.PatchReleases)
            {
                var patchYear = patch.ReleaseDate.Year.ToString();
                var patchMonth = patch.ReleaseDate.Month.ToString("D2");
                var patchDay = patch.ReleaseDate.Day.ToString("D2");

                if (!years.TryGetValue(patchYear, out var releaseYear))
                {
                    releaseYear = new ReleaseYear(patchYear, []);
                    years[patchYear] = releaseYear;
                }

                if (!releaseYear.Months.TryGetValue(patchMonth, out var releaseMonth))
                {
                    // Create a new ReleaseMonth for this month if it doesn't exist
                    releaseMonth = new ReleaseMonth(patchMonth, []);
                    releaseYear.Months[patchMonth] = releaseMonth;
                }

                if (!releaseMonth.Days.TryGetValue(patchDay, out var releaseDay))
                {
                    // Create a new ReleaseDay for this day if it doesn't exist
                    releaseDay = new ReleaseDay(
                        new DateOnly(patch.ReleaseDate.Year, patch.ReleaseDate.Month, patch.ReleaseDate.Day),
                        patchMonth,
                        patchDay,
                        []);
                    releaseMonth.Days[patchDay] = releaseDay;
                }

                releaseDay.Releases.Add(patch);
            }
        }

        return new ReleaseHistory(years);
    }

    public static void PopulateCveInformation(ReleaseHistory releaseHistory, string rootDir)
    {
        var historyDir = Path.Combine(rootDir, "archives");
        if (!Directory.Exists(historyDir))
        {
            return;
        }

        foreach (var year in releaseHistory.Years.Values)
        {
            foreach (var month in year.Months.Values)
            {
                foreach (var day in month.Days.Values)
                {
                    var relativePath = Path.Combine(year.Year, month.Month, "cve.json");
                    var cveJsonPath = Path.Combine(historyDir, relativePath);
                    if (File.Exists(cveJsonPath))
                    {
                        day.CveJson = relativePath;
                    }
                }
            }
        }
    }
}
