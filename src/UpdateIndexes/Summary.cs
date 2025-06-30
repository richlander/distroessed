using DotnetRelease;
using System.Collections.Generic;

namespace UpdateIndexes;

public class Summary(MajorReleaseOverview major)
{
    private readonly MajorReleaseOverview _major = major;

    public void Process()
    {
        var version = _major.ChannelVersion;
        var supportPhase = _major.SupportPhase;
        var supported = supportPhase is SupportPhase.Active or SupportPhase.Maintenance or SupportPhase.Preview or SupportPhase.GoLive;
        var eolDate = _major.EolDate;
        var releaseType = _major.ReleaseType;
        var latestReleaseDate = _major.LatestReleaseDate;
        string shortVersion = version.Substring(0, version.IndexOf('.'));
        int verNum = int.Parse(shortVersion);

        if (version is null || verNum < 8)
        {
            return;
        }

        var bands = new Dictionary<string, SdkBand>(StringComparer.Ordinal);
        var alt = bands.GetAlternateLookup<ReadOnlySpan<char>>();

        bool latest = true;

        foreach (var release in _major.Releases)
        {
            var date = release.ReleaseDate;
            foreach (var sdk in release.Sdks)
            {
                ReadOnlySpan<char> ver = sdk.Version;
                var key = ver[..5];

                if (!alt.ContainsKey(key))
                {
                    bands.Add(key.ToString(), new SdkBand(sdk.Version, $".NET SDK {key}xx")
                    {
                        SupportPhase = supported && date == latestReleaseDate ? supportPhase : SupportPhase.Eol,
                        LatestReleaseDate = date
                    });
                }

                if (latest)
                {
                    latest = false;
                }
            }
        }

        foreach (var b in bands.Values)
        {
            Console.WriteLine(b);
        }
    }
}

public record SdkBand
(
    string Version,
    string Label
)
{
    public SupportPhase SupportPhase { get; set; }
    public DateOnly LatestReleaseDate { get; set; }
}
