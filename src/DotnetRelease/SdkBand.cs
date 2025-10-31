using System.Net;

namespace DotnetRelease;

public record SdkBand
(
    string Version,
    string Label
)
{
    public SupportPhase SupportPhase { get; set; }
    public DateOnly LatestReleaseDate { get; set; }

    public static IList<SdkBand> GetSdkBandsForMajorRelease(MajorReleaseOverview major)
    {
        var version = major.ChannelVersion;
        
        if (version is null)
        {
            return Array.Empty<SdkBand>();
        }
        
        var supportPhase = major.SupportPhase;
        var supported = supportPhase is SupportPhase.Active or SupportPhase.Maintenance or SupportPhase.Preview;
        var eolDate = major.EolDate;
        var releaseType = major.ReleaseType;
        var latestReleaseDate = major.LatestReleaseDate;
        string shortVersion = version.Substring(0, version.IndexOf('.'));
        int verNum = int.Parse(shortVersion);

        if (verNum < 8)
        {
            return Array.Empty<SdkBand>();
        }

        var bands = new Dictionary<string, SdkBand>(StringComparer.Ordinal);

        bool latest = true;

        foreach (var release in major.Releases)
        {
            var date = release.ReleaseDate;
            foreach (var sdk in release.Sdks)
            {
                var key = sdk.Version[..5];

                if (!bands.ContainsKey(key))
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

        return [.. bands.Values];
    }
}
