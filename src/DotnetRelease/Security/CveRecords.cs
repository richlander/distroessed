using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetRelease.Security;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("A set of CVEs with affected products, packages, and commit information.")]
public record CveRecords(
    [property: Description("Date when the CVE set was last updated.")]
    string LastUpdated,

    [property: Description("Title of the CVE disclosure.")]
    string Title,

    [property: Description("Set of CVEs disclosed."),
        JsonPropertyName("disclosures")]
    IList<Cve> Disclosures,

    [property: Description("Set of products affected by CVEs.")]
    IList<Product> Products,

    [property: Description("Set of packages affected by CVEs.")]
    IList<Package> Packages,

    [property: Description("Dictionary of commit information, keyed by commit hash."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IDictionary<string, CommitInfo>? Commits = null,

    [property: Description("Dictionary of product display names, keyed by product name.")]
    IDictionary<string, string>? ProductName = null,

    [property: Description("Dictionary of CVE IDs affecting each product, keyed by product name.")]
    IDictionary<string, IList<string>>? ProductCves = null,

    [property: Description("Dictionary of CVE IDs affecting each package, keyed by package name.")]
    IDictionary<string, IList<string>>? PackageCves = null,

    [property: Description("Dictionary of CVE IDs affecting each release, keyed by release version.")]
    IDictionary<string, IList<string>>? ReleaseCves = null,

    [property: Description("Dictionary of release versions affected by each CVE, keyed by CVE ID.")]
    IDictionary<string, IList<string>>? CveReleases = null,

    [property: Description("Dictionary of commit hashes that fix each CVE, keyed by CVE ID."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IDictionary<string, IList<string>>? CveCommits = null
);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
[Description("A disclosed vulnerability (CVE).")]
public record Cve(
    [property: Description("The CVE ID.")]
    string Id,

    [property: Description("Brief description of the vulnerability type.")]
    string Problem,

    [property: Description("Detailed description of the vulnerability.")]
    IList<string> Description,

    [property: Description("CVSS score and vector string.")]
    Cvss Cvss,

    [property: Description("Timeline of when the CVE was disclosed and fixed.")]
    Timeline Timeline,

    [property: Description("Platforms affected by the CVE.")]
    IList<string> Platforms,

    [property: Description("Architectures affected by the CVE.")]
    IList<string> Architectures,

    [property: Description("Reference URLs for the CVE.")]
    IList<string> References,

    [property: Description("Mitigation information for the CVE."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IList<string>? Mitigation = null,

    [property: Description("CWE (Common Weakness Enumeration) identifier."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Weakness = null,

    [property: Description("CVE Numbering Authority information.")]
    Cna? Cna = null
);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("Timeline of CVE lifecycle events.")]
public record Timeline(
    [property: Description("Date when the CVE was publicly disclosed.")]
    Event Disclosure,

    [property: Description("Date when the CVE fix was released."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    Event? Fixed = null,

    [property: Description("Additional timeline events (e.g., reported, embargo start, validation)."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IList<Event>? Other = null
);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("A dated event in the CVE lifecycle.")]
public record Event(
    [property: Description("Date of the event.")]
    DateOnly Date,

    [property: Description("Description of what occurred on this date.")]
    string Description
);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("CVSS scoring information for the CVE.")]
public record Cvss(
    [property: Description("CVSS version used for scoring.")]
    string Version,

    [property: Description("CVSS vector string.")]
    string Vector,

    [property: Description("CVSS base score."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    decimal Score = 0.0m,

    [property: Description("CVSS severity rating (low/medium/high/critical) derived from base score."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    string Severity = "",

    [property: Description("Source of the CVSS score calculation (e.g., 'microsoft', 'nvd')."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Source = null,

    [property: Description("CVSS temporal score, adjusted for exploit maturity, remediation level, and report confidence."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    decimal? TemporalScore = null
);

[Description("CVE Numbering Authority information.")]
[JsonConverter(typeof(CnaJsonConverter))]
public record Cna(
    [property: Description("Name of the CVE Numbering Authority.")]
    string Name,

    [property: Description("CNA-specific severity rating (e.g., Microsoft's 'Low', 'Moderate', 'Important', 'Critical'). This is the vendor's business risk assessment, which may differ from CVSS severity."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Severity = null,

    [property: Description("Impact type/category of the vulnerability as defined by the CNA (e.g., 'Denial of Service', 'Elevation of Privilege', 'Remote Code Execution', 'Information Disclosure')."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Impact = null,

    [property: Description("Acknowledgments for researchers who discovered or reported the CVE."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IList<string>? Acknowledgments = null,

    [property: Description("Frequently Asked Questions from the CNA about this CVE."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IList<CnaFaq>? Faq = null
);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("FAQ entry from the CVE Numbering Authority.")]
public record CnaFaq(
    [property: Description("The question being asked.")]
    string Question,

    [property: Description("The answer to the question.")]
    string Answer
);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("A product affected by a CVE.")]
public record Product(
    [property: Description("The CVE ID affecting this product.")]
    string CveId,

    [property: Description("Name of the affected product.")]
    string Name,

    [property: Description("Minimum vulnerable version of the product.")]
    string MinVulnerable,

    [property: Description("Maximum vulnerable version of the product.")]
    string MaxVulnerable,

    [property: Description("Version of the product that contains the fix.")]
    string Fixed,

    [property: Description("Major release version affected.")]
    string Release,

    [property: Description("List of commit hashes that fix the vulnerability.")]
    IList<string> Commits
);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("A package affected by a CVE.")]
public record Package(
    [property: Description("The CVE ID affecting this package.")]
    string CveId,

    [property: Description("Name of the affected package.")]
    string Name,

    [property: Description("Minimum vulnerable version of the package.")]
    string MinVulnerable,

    [property: Description("Maximum vulnerable version of the package.")]
    string MaxVulnerable,

    [property: Description("Version of the package that contains the fix.")]
    string Fixed,

    [property: Description("Major release version affected.")]
    string Release,

    [property: Description("List of commit hashes that fix the vulnerability.")]
    IList<string> Commits
);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("Information about a commit that fixes a CVE.")]
public record CommitInfo(
    [property: Description("Repository name where the commit exists.")]
    string Repo,

    [property: Description("Branch name where the commit exists.")]
    string Branch,

    [property: Description("Commit hash (SHA).")]
    string Hash,

    [property: Description("Organization that owns the repository.")]
    string Org,

    [property: Description("URL to the commit diff.")]
    string Url
);
