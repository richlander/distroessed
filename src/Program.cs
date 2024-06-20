using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using DotnetSupport;
using EndOfLifeDate;

HttpClient client= new();
DateOnly threeMonthsDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3));

SupportMatrix? matrix = await SupportedOS.GetSupportMatrix(client);
foreach (SupportFamily family in matrix?.Families ?? throw new Exception())
{
    Console.WriteLine($"**{family.Name}**");

    foreach (SupportDistribution distro in family.Distributions)
    {
        IList<SupportCycle>? cycles = await EndOfLifeDate.EndOfLifeDate.GetProduct(client, distro.Id) ?? throw new Exception();
        List<SupportCycle> unsupportedActiveRelease = [];
        List<SupportCycle> soonEolReleases = [];
        List<SupportCycle> supportedEolReleases = [];
        int activeReleases = 0;

        foreach (SupportCycle cycle in cycles)
        {
            SupportInfo support = cycle.GetSupportInfo();
            bool distroCycleListed = distro.SupportedCycles.Contains(cycle.Cycle);

            if (!support.Active)
            {
                if (distroCycleListed)
                {
                    supportedEolReleases.Add(cycle);
                }

                continue;
            }

            activeReleases++;

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

        Console.WriteLine($" {distro.Name}");
        Console.WriteLine($"  Releases active : {activeReleases}");
        Console.WriteLine($"  Unsupported active releases: {unsupportedActiveRelease.Count}");
        Console.WriteLine($"  Releases EOL soon: {soonEolReleases.Count}");
        Console.WriteLine($"  Supported inactive releases: {supportedEolReleases.Count}");

        PrintMessageAboutCycles(unsupportedActiveRelease.Count > 0, unsupportedActiveRelease, "Releases that are active but not supported:", 2);
        PrintMessageAboutCycles(soonEolReleases.Count > 0, soonEolReleases, "Releases that are EOL within 2 months:", 2);
        PrintMessageAboutCycles(supportedEolReleases.Count > 0, supportedEolReleases, "Releases that are EOL but supported:", 2);

        Console.WriteLine();
    }
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
