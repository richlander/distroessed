using DotnetRelease;

if (args.Length is 0 || !int.TryParse(args[0], out int majorVersion))
{
    ReportInvalidArgs();
    return;
}

string? baseUrl = args.Length > 1 ? args[1] : null;
var report = await ReleaseReportGenerator.GetReportOverviewAsync(majorVersion, baseUrl);
var reportJson = ReleaseReport.WriteReport(report);

Console.WriteLine(reportJson);


static void ReportInvalidArgs()
{
    Console.WriteLine("Invalid args.");
    Console.WriteLine("Expected: version [URL or Path, absolute or root location]");
}
