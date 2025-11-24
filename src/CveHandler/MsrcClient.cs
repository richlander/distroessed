using System.Text.RegularExpressions;
using System.Xml.Linq;
using DotnetRelease.Security;

namespace CveHandler;

/// <summary>
/// Client for fetching and parsing CVE data from Microsoft Security Response Center (MSRC) API
/// </summary>
public static class MsrcClient
{
    /// <summary>
    /// Fetches MSRC data for a specific security update identifier
    /// </summary>
    /// <param name="msrcId">MSRC identifier (e.g., "2024-Jan")</param>
    /// <returns>Dictionary of CVE data keyed by CVE ID, or null if fetch fails</returns>
    public static async Task<Dictionary<string, MsrcCveData>?> FetchDataAsync(string msrcId)
    {
        using var httpClient = new HttpClient();
        var url = $"https://api.msrc.microsoft.com/cvrf/v2.0/cvrf/{msrcId}";
        
        try
        {
            var xmlContent = await httpClient.GetStringAsync(url);
            return ParseXml(xmlContent);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Parses MSRC XML to extract CVE metadata
    /// </summary>
    private static Dictionary<string, MsrcCveData> ParseXml(string xmlContent)
    {
        var result = new Dictionary<string, MsrcCveData>();
        
        // Parse embedded HTML table from DocumentNotes
        var tableMatch = Regex.Match(xmlContent, @"&lt;table&gt;.*?&lt;/table&gt;", RegexOptions.Singleline);
        if (!tableMatch.Success)
            return result;

        var tableHtml = tableMatch.Value
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&amp;", "&");

        // Extract rows
        var rowMatches = Regex.Matches(tableHtml, @"<tr>(.*?)</tr>", RegexOptions.Singleline);
        
        foreach (Match rowMatch in rowMatches)
        {
            var row = rowMatch.Groups[1].Value;
            var cells = Regex.Matches(row, @"<td>(.*?)</td>", RegexOptions.Singleline)
                .Select(m => Regex.Replace(m.Groups[1].Value, @"<a[^>]*>(.*?)</a>", "$1").Trim())
                .ToList();

            if (cells.Count >= 4 && cells[1].StartsWith("CVE-"))
            {
                var cveId = cells[1];
                var scoreText = cells[2];
                var vector = cells[3];

                if (decimal.TryParse(scoreText, out decimal score))
                {
                    result[cveId] = new MsrcCveData
                    {
                        CveId = cveId,
                        Score = score,
                        Vector = vector,
                        Impact = "",
                        Weakness = null,
                        CnaSeverity = null
                    };
                }
            }
        }

        // Parse XML for Impact, Weakness (CWE), and MSRC Severity
        var xdoc = XDocument.Parse(xmlContent);
        XNamespace vulnNs = "http://www.icasi.org/CVRF/schema/vuln/1.1";

        foreach (var vuln in xdoc.Descendants(vulnNs + "Vulnerability"))
        {
            var cveElem = vuln.Element(vulnNs + "CVE");
            if (cveElem is null) continue;

            var cveId = cveElem.Value;
            if (!result.ContainsKey(cveId)) continue;

            // Get Impact
            var impactElem = vuln.Descendants(vulnNs + "Threat")
                .FirstOrDefault(t => t.Attribute("Type")?.Value == "Impact");
            if (impactElem is not null)
            {
                var impactDesc = impactElem.Element(vulnNs + "Description")?.Value ?? "";
                result[cveId] = result[cveId] with { Impact = impactDesc };
            }

            // Get CNA Severity
            var severityElem = vuln.Descendants(vulnNs + "Threat")
                .FirstOrDefault(t => t.Attribute("Type")?.Value == "Severity");
            if (severityElem is not null)
            {
                var severityDesc = severityElem.Element(vulnNs + "Description")?.Value ?? "";
                result[cveId] = result[cveId] with { CnaSeverity = severityDesc };
            }

            // Get CWE
            var cweElem = vuln.Element(vulnNs + "CWE");
            if (cweElem is not null)
            {
                var cweId = cweElem.Attribute("ID")?.Value;
                if (cweId is not null)
                {
                    result[cveId] = result[cveId] with { Weakness = cweId };
                }
            }

            // Get Acknowledgments
            var acknowledgments = new List<string>();
            var acknowledgementsElem = vuln.Element(vulnNs + "Acknowledgments");
            if (acknowledgementsElem is not null)
            {
                foreach (var ackElem in acknowledgementsElem.Elements(vulnNs + "Acknowledgment"))
                {
                    var nameElem = ackElem.Element(vulnNs + "Name");
                    if (nameElem is not null)
                    {
                        var name = Regex.Replace(nameElem.Value, @"<[^>]+>", "");
                        name = name.Replace("&amp;", "&").Trim();
                        if (!string.IsNullOrEmpty(name) && !acknowledgments.Contains(name))
                        {
                            acknowledgments.Add(name);
                        }
                    }
                }
            }
            if (acknowledgments.Count > 0)
            {
                result[cveId] = result[cveId] with { Acknowledgments = acknowledgments };
            }

            // Get FAQs
            var faqs = new List<CnaFaq>();
            var notesElems = vuln.Elements(vulnNs + "Notes");
            foreach (var notesElem in notesElems)
            {
                var faqNotes = notesElem.Elements(vulnNs + "Note")
                    .Where(n => n.Attribute("Type")?.Value == "FAQ");
                
                foreach (var faqNote in faqNotes)
                {
                    var htmlContent = faqNote.Value;
                    var questionMatch = Regex.Match(htmlContent, @"<strong>(.*?)</strong>", RegexOptions.Singleline);
                    var answerMatch = Regex.Match(htmlContent, @"</strong>\s*</p>\s*<p>(.*?)</p>", RegexOptions.Singleline);
                    
                    if (questionMatch.Success)
                    {
                        var question = Regex.Replace(questionMatch.Groups[1].Value, @"<[^>]+>", "").Trim();
                        question = question.Replace("&amp;", "&");
                        
                        var answer = answerMatch.Success 
                            ? Regex.Replace(answerMatch.Groups[1].Value, @"<[^>]+>", "").Trim()
                            : "";
                        answer = answer.Replace("&amp;", "&");
                        
                        if (!string.IsNullOrEmpty(question) && !string.IsNullOrEmpty(answer))
                        {
                            faqs.Add(new CnaFaq(question, answer));
                        }
                    }
                }
            }
            if (faqs.Count > 0)
            {
                result[cveId] = result[cveId] with { Faqs = faqs };
            }
        }

        return result;
    }
}

/// <summary>
/// CVE metadata from Microsoft Security Response Center
/// </summary>
public record MsrcCveData
{
    required public string CveId { get; init; }
    required public decimal Score { get; init; }
    required public string Vector { get; init; }
    required public string Impact { get; init; }
    public string? Weakness { get; init; }
    public string? CnaSeverity { get; init; }
    public List<string>? Acknowledgments { get; init; }
    public List<CnaFaq>? Faqs { get; init; }
}
