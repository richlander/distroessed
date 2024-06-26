using System.Text.Json.Serialization;

namespace EndOfLifeDate;

public record SupportProduct(SupportCycle[] Cycles);
public record SupportCycle(string Cycle, string Codename, DateOnly ReleaseDate, string? Link)
{
    [JsonConverter(typeof(EolStringConverter))]
    public string? Eol { get; set; }

    [JsonConverter(typeof(EolStringConverter))]
    public string? Lts { get; set; }

    public string? LatestReleaseDate { get; set; }

    public SupportInfo GetSupportInfo()
    {
        if (Eol is "False")
        {
            return new(true, DateOnly.MaxValue);
        }
        else if (Eol is not null && DateOnly.TryParse(Eol, out DateOnly eolDate))
        {
            bool isActive = eolDate > DateOnly.FromDateTime(DateTime.UtcNow);            
            return new(isActive, eolDate);
        }

        return new(false, DateOnly.MinValue);
    }
};

public record struct SupportInfo(bool IsActive, DateOnly EolDate);

/*
[
    {
        "cycle": "8",
        "releaseDate": "2023-11-14",
        "lts": true,
        "eol": "2026-11-10",
        "latest": "8.0.6",
        "latestReleaseDate": "2024-05-29"
    },
    {
        "cycle": "7",
        "releaseDate": "2022-11-08",
        "eol": "2024-05-14",
        "latest": "7.0.20",
        "latestReleaseDate": "2024-05-29",
        "lts": false
    },
    ....
*/
