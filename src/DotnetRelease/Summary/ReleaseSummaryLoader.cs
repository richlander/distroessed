using System.Globalization;
using System.Text.Json;
using DotnetRelease.Graph;
using DotnetRelease.ReleaseInfo;

namespace DotnetRelease.Summary;

public static class ReleaseSummaryLoader
{
    public static async Task<List<MajorReleaseSummary>> GetReleaseSummariesAsync(string rootDir)
    {
        var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);

        // List of major version entries
        List<MajorReleaseSummary> majorEntries = [];

        // look at all the major version directories
        foreach (var majorVersionDir in Directory.EnumerateDirectories(rootDir).OrderDescending(numericStringComparer))
        {
            // The presence of a releases.json file indicates this is a major version directory
            var releasesJson = Path.Combine(majorVersionDir, FileNames.Releases);
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

                // Determine the correct path for the patch - previews/RCs are in a different structure
                string patchDir;
                if (release.ReleaseVersion.Contains("-preview.") || release.ReleaseVersion.Contains("-rc."))
                {
                    // Extract preview/rc number: "10.0.0-preview.1" -> "preview1" or "10.0.0-rc.1" -> "rc1"
                    var dashIndex = release.ReleaseVersion.IndexOf('-');
                    if (dashIndex > 0)
                    {
                        var suffix = release.ReleaseVersion.Substring(dashIndex + 1); // "preview.1" or "rc.1"
                        var parts = suffix.Split('.');
                        if (parts.Length >= 2)
                        {
                            var previewOrRc = parts[0]; // "preview" or "rc"
                            var number = parts[1];       // "1", "2", etc.
                            var subdir = $"{previewOrRc}{number}"; // "preview1" or "rc1"
                            patchDir = Path.Combine(majorVersionDir, "preview", subdir);
                        }
                        else
                        {
                            patchDir = Path.Combine(majorVersionDir, release.ReleaseVersion);
                        }
                    }
                    else
                    {
                        patchDir = Path.Combine(majorVersionDir, release.ReleaseVersion);
                    }
                }
                else
                {
                    patchDir = Path.Combine(majorVersionDir, release.ReleaseVersion);
                }

                var patchJson = Path.Combine(patchDir, FileNames.Release);
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
                    var label = $".NET SDK {version}";
                    components.Add(new ReleaseComponent("SDK", version, label));
                }

                if (release?.ReleaseVersion == null)
                {
                    throw new InvalidOperationException($"Release version is null in {patchJson}");
                }

                PatchReleaseSummary summary = new(major.ChannelVersion, release.ReleaseVersion, release.ReleaseDate, isSecurity, release.CveList, components)
                {
                    ReleaseJsonPath = patchExists ? Path.GetRelativePath(rootDir, patchJson) : null,
                    PatchDirPath = Directory.Exists(patchDir) ? Path.GetRelativePath(rootDir, patchDir) : null
                };
                patchEntries.Add(summary);
            }

            Console.WriteLine($"Patch releases found for .NET {majorVersionDirName}: {patchEntries.Count}");

            IList<PatchReleaseSummary> patchVersions = patchEntries.Count is 0 ? Array.Empty<PatchReleaseSummary>() : patchEntries;

            // Read lifecycle data from _manifest.json (authoritative source)
            var manifestPath = Path.Combine(majorVersionDir, FileNames.PartialManifest);
            PartialManifest? partialManifest = null;
            if (File.Exists(manifestPath))
            {
                try
                {
                    var manifestJson = await File.ReadAllTextAsync(manifestPath);
                    partialManifest = JsonSerializer.Deserialize<PartialManifest>(manifestJson, ReleaseManifestSerializerContext.Default.PartialManifest);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to read {manifestPath}: {ex.Message}");
                }
            }

            // Use _manifest.json dates if available, otherwise fall back to releases.json
            DateTimeOffset gaDate;
            DateTimeOffset eolDate;
            ReleaseType releaseType;
            SupportPhase phase;

            if (partialManifest?.GaDate.HasValue == true && partialManifest?.EolDate.HasValue == true)
            {
                gaDate = partialManifest.GaDate.Value;
                eolDate = partialManifest.EolDate.Value;
                releaseType = partialManifest.ReleaseType ?? major.ReleaseType;
                phase = ReleaseStability.ComputeEffectivePhase(
                    partialManifest.Phase ?? major.SupportPhase,
                    gaDate);
            }
            else
            {
                // Fallback: use releases.json data
                eolDate = new DateTimeOffset(major.EolDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                releaseType = major.ReleaseType;
                phase = major.SupportPhase;

                // For GA date, we don't have a good source without _manifest.json
                // Use MinValue to indicate unknown
                gaDate = DateTimeOffset.MinValue;
                Console.WriteLine($"Warning: {majorVersionDirName} - No _manifest.json found, GA date unknown");
            }

            // Create Lifecycle object with all lifecycle information
            var lifecycle = new Lifecycle(releaseType, phase, gaDate, eolDate);
            lifecycle.Supported = ReleaseStability.IsSupported(lifecycle);

            MajorReleaseSummary majorSummary = new MajorReleaseSummary(
                major.ChannelVersion,
                $".NET " + major.ChannelVersion,
                lifecycle,
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
        var historyDir = Path.Combine(rootDir, FileNames.Directories.Timeline);
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
                    var relativePath = Path.Combine(year.Year, month.Month, FileNames.Cve);
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
