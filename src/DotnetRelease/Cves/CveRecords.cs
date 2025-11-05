using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease.Cves;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("A set of CVEs with affected products, packages, and commit information.")]
public record CveRecords(
    [property: Description("Date when the CVE set was last updated.")]
    string LastUpdated,

    [property: Description("Title of the CVE disclosure.")]
    string Title,

    [property: Description("Set of CVEs disclosed.")]
    IList<Cve> Cves,

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

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("A disclosed vulnerability (CVE).")]
public record Cve(
    [property: Description("The CVE ID.")]
    string Id,

    [property: Description("Brief description of the vulnerability type.")]
    string Problem,

    [property: Description("Severity level of the CVE.")]
    string Severity,

    [property: Description("Timeline of when the CVE was disclosed and fixed.")]
    Timeline Timeline,

    [property: Description("CVSS score and vector string.")]
    Cvss Cvss,

    [property: Description("Detailed description of the vulnerability.")]
    IList<string> Description,

    [property: Description("Platforms affected by the CVE.")]
    IList<string> Platforms,

    [property: Description("Architectures affected by the CVE.")]
    IList<string> Architectures,

    [property: Description("CVE Numbering Authority that assigned the CVE.")]
    string Cna,

    [property: Description("Reference URLs for the CVE.")]
    IList<string> References,

    [property: Description("Mitigation information for the CVE."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IList<string>? Mitigation = null
);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("Timeline of CVE disclosure and fix.")]
public record Timeline(
    [property: Description("Date when the CVE was publicly disclosed.")]
    string Disclosed,

    [property: Description("Date when the CVE fix was released.")]
    string Fixed
);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("CVSS scoring information for the CVE.")]
public record Cvss(
    [property: Description("CVSS version used for scoring.")]
    string Version,

    [property: Description("CVSS vector string.")]
    string Vector
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
