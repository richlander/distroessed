#!/usr/bin/env python3
"""
Generate cve.json file for a specific month using releases.json and GitHub announcements.

Usage: python generate_cve_month.py 2023 10
"""

import json
import re
import sys
import time
from dataclasses import dataclass, asdict
from pathlib import Path
from urllib.request import urlopen, Request
from urllib.error import HTTPError
from collections import defaultdict


CORE_REPO = Path("/home/rich/git/core")
RELEASES_DIR = CORE_REPO / "release-notes"
TIMELINE_DIR = RELEASES_DIR / "timeline"


@dataclass
class PackageInfo:
    cve_id: str
    name: str
    min_vulnerable: str
    max_vulnerable: str
    fixed: str
    release: str


def fetch_github_issue(issue_number: int) -> dict | None:
    """Fetch a GitHub issue by number."""
    url = f"https://api.github.com/repos/dotnet/announcements/issues/{issue_number}"
    req = Request(url, headers={"User-Agent": "cve-fetcher"})
    try:
        with urlopen(req) as response:
            return json.loads(response.read())
    except HTTPError as e:
        print(f"  Error fetching issue {issue_number}: {e}", file=sys.stderr)
        return None


def search_cve_issues(cve_id: str) -> list[int]:
    """Search for GitHub issues mentioning a CVE ID."""
    url = f"https://api.github.com/search/issues?q={cve_id}+repo:dotnet/announcements+is:issue"
    req = Request(url, headers={"User-Agent": "cve-fetcher"})
    try:
        with urlopen(req) as response:
            data = json.loads(response.read())
            return [item["number"] for item in data.get("items", [])]
    except HTTPError as e:
        print(f"  Error searching for {cve_id}: {e}", file=sys.stderr)
        return []


def extract_description(body: str) -> str:
    """Extract the vulnerability description from announcement body."""
    body = body.replace('\r\n', '\n').replace('\r', '\n')

    def clean_description(text: str) -> str:
        text = re.sub(r'\[([^\]]+)\]\([^)]+\)', r'\1', text)
        text = re.sub(r'Microsoft is releasing this security advisory[^.]*\.', '', text, flags=re.IGNORECASE)
        text = re.sub(r'This advisory also provides guidance[^.]*\.', '', text, flags=re.IGNORECASE)
        return text.strip()

    patterns = [
        r'#{2,3}\s*.*?Executive [Ss]ummary.*?\n\n(.*?)(?=\n#{2,3})',
        r'#{2,3}\s*.*?Executive [Ss]ummary.*?\n(.*?)(?=\n#{2,3})',
    ]

    for pattern in patterns:
        match = re.search(pattern, body, re.IGNORECASE | re.DOTALL)
        if match:
            desc = clean_description(match.group(1))
            if desc:
                paragraphs = [p.strip() for p in desc.split('\n\n') if p.strip() and len(p.strip()) > 50]
                for p in paragraphs:
                    if 'vulnerability' in p.lower() or 'susceptible' in p.lower() or 'exists' in p.lower():
                        return p
                if paragraphs:
                    return paragraphs[0]

    match = re.search(r'Executive [Ss]ummary\s*\n+(.*?)(?=\n#|\n\*\*)', body, re.DOTALL)
    if match:
        desc = clean_description(match.group(1))
        paragraphs = [p.strip() for p in desc.split('\n\n') if p.strip() and len(p.strip()) > 50]
        for p in paragraphs:
            if 'vulnerability' in p.lower():
                return p
        if paragraphs:
            return paragraphs[-1]

    match = re.search(r'# Microsoft Security Advisory.*?\n+(.*?)(?=\n###|\n##)', body, re.DOTALL)
    if match:
        desc = clean_description(match.group(1))
        if desc:
            paragraphs = [p.strip() for p in desc.split('\n\n') if p.strip() and len(p.strip()) > 50]
            for p in paragraphs:
                if 'vulnerability' in p.lower() or 'aware of' in p.lower():
                    return p
            if paragraphs:
                return paragraphs[0]

    return ""


def extract_problem_type(body: str, title: str) -> str:
    """Extract the problem/vulnerability type."""
    patterns = [
        (r'Denial of Service', 'Denial of Service'),
        (r'Remote Code Execution', 'Remote Code Execution'),
        (r'Elevation of Privilege', 'Elevation of Privilege'),
        (r'Information Disclosure', 'Information Disclosure'),
        (r'Security Feature Bypass', 'Security Feature Bypass'),
        (r'Spoofing', 'Spoofing'),
        (r'Tampering', 'Tampering'),
    ]

    for pattern, result in patterns:
        if re.search(pattern, title, re.IGNORECASE):
            return result
        if re.search(pattern, body, re.IGNORECASE):
            return result

    return "Security Vulnerability"


def extract_platforms(body: str) -> list[str]:
    """Extract affected platforms from announcement."""
    body_lower = body.lower()

    if "only affects linux" in body_lower or "this issue only affects linux" in body_lower:
        return ["linux"]
    if "only affects windows" in body_lower or "this issue only affects windows" in body_lower:
        return ["windows"]
    if "only affects macos" in body_lower:
        return ["macos"]

    return ["all"]


def parse_packages_from_body(body: str, cve_id: str) -> list[PackageInfo]:
    """Parse package tables from announcement body."""
    packages = []
    pattern = r'\[([^\]]+)\]\([^)]+\)\s*\|\s*([^|]+)\s*\|\s*(\S+)'

    for match in re.finditer(pattern, body):
        pkg_name = match.group(1).strip()
        version_range = match.group(2).strip()
        fixed = match.group(3).strip()

        # Skip platform-specific runtime packages - these go in products, not packages
        if pkg_name.startswith("Microsoft.NETCore.App.Runtime."):
            continue
        if pkg_name.startswith("Microsoft.AspNetCore.App.Runtime."):
            continue
        if pkg_name.startswith("Microsoft.WindowsDesktop.App.Runtime."):
            continue

        # Parse version range
        min_ver = ""
        max_ver = ""
        parts = version_range.split(",")
        for part in parts:
            part = part.strip()
            if ">=" in part:
                min_ver = part.replace(">=", "").strip()
            elif "<=" in part:
                max_ver = part.replace("<=", "").strip()
            elif "<" in part:
                max_ver = part.replace("<", "").strip()

        # Extract release from fixed version
        fixed_parts = fixed.split(".")
        release = f"{fixed_parts[0]}.{fixed_parts[1]}" if len(fixed_parts) >= 2 else fixed

        packages.append(PackageInfo(
            cve_id=cve_id,
            name=pkg_name,
            min_vulnerable=min_ver,
            max_vulnerable=max_ver,
            fixed=fixed,
            release=release
        ))

    return packages


def get_cve_announcement_data(cve_id: str) -> dict | None:
    """Fetch and parse CVE data from GitHub announcement."""
    issue_numbers = search_cve_issues(cve_id)
    time.sleep(0.3)

    if not issue_numbers:
        return None

    for issue_num in issue_numbers:
        issue = fetch_github_issue(issue_num)
        time.sleep(0.3)

        if not issue:
            continue

        body = issue.get("body", "")
        title = issue.get("title", "")

        if cve_id not in title:
            continue

        return {
            "cve_id": cve_id,
            "issue_number": issue_num,
            "description": extract_description(body),
            "problem": extract_problem_type(body, title),
            "platforms": extract_platforms(body),
            "packages": parse_packages_from_body(body, cve_id),
            "announcement_url": f"https://github.com/dotnet/announcements/issues/{issue_num}"
        }

    return None


def get_releases_for_month(year: int, month: int) -> dict:
    """Get all releases and CVEs for a specific month from releases.json files."""
    month_str = f"{year}-{month:02d}"
    releases = []

    for version_dir in RELEASES_DIR.iterdir():
        if not version_dir.is_dir() or not version_dir.name[0].isdigit():
            continue

        releases_file = version_dir / "releases.json"
        if not releases_file.exists():
            continue

        with open(releases_file) as f:
            data = json.load(f)

        for release in data.get("releases", []):
            release_date = release.get("release-date", "")
            if release_date.startswith(month_str):
                cve_list = release.get("cve-list", [])
                if cve_list:
                    releases.append({
                        "version": data.get("channel-version"),
                        "release_version": release.get("release-version"),
                        "release_date": release_date,
                        "cves": [c.get("cve-id").strip() for c in cve_list if c.get("cve-id")]
                    })

    return releases


def generate_cve_json(year: int, month: int) -> dict:
    """Generate the full cve.json structure for a month."""
    print(f"Generating cve.json for {year}/{month:02d}...", file=sys.stderr)

    # Get releases for this month
    releases = get_releases_for_month(year, month)
    if not releases:
        print(f"  No security releases found for {year}-{month:02d}", file=sys.stderr)
        return None

    # Collect unique CVEs and their release info
    cve_releases = defaultdict(list)  # cve_id -> [releases]
    release_dates = {}  # cve_id -> earliest release date

    for rel in releases:
        for cve_id in rel["cves"]:
            cve_releases[cve_id].append({
                "release": rel["version"],
                "release_version": rel["release_version"],
                "release_date": rel["release_date"]
            })
            if cve_id not in release_dates or rel["release_date"] < release_dates[cve_id]:
                release_dates[cve_id] = rel["release_date"]

    # Determine the primary release date for the file
    all_dates = sorted(set(release_dates.values()))
    primary_date = all_dates[0] if all_dates else f"{year}-{month:02d}-01"

    # Fetch announcement data for each CVE
    disclosures = []
    all_packages = []

    for cve_id in sorted(cve_releases.keys()):
        print(f"  Fetching {cve_id}...", file=sys.stderr)
        announcement = get_cve_announcement_data(cve_id)

        disclosure_date = release_dates.get(cve_id, primary_date)

        disclosure = {
            "id": cve_id,
            "problem": f".NET {announcement['problem']} Vulnerability" if announcement else "Security Vulnerability",
            "description": [announcement["description"]] if announcement and announcement["description"] else [f"A security vulnerability exists in .NET. See {cve_id} for details."],
            "timeline": {
                "disclosure": {
                    "date": disclosure_date,
                    "description": "Publicly disclosed"
                },
                "fixed": {
                    "date": disclosure_date,
                    "description": "Fix released"
                }
            },
            "platforms": announcement["platforms"] if announcement else ["all"],
            "architectures": ["all"],
            "references": [announcement["announcement_url"]] if announcement else [f"https://msrc.microsoft.com/update-guide/vulnerability/{cve_id}"],
            "cna": {
                "name": "microsoft",
                "impact": announcement["problem"] if announcement else "Unknown"
            }
        }

        disclosures.append(disclosure)

        # Collect packages from announcement
        if announcement:
            all_packages.extend(announcement["packages"])

    # Build products list from releases.json data
    products = []
    for cve_id, rels in cve_releases.items():
        for rel in rels:
            # Default to dotnet-runtime - this is imperfect but better than nothing
            # ASP.NET CVEs could be detected by announcement content
            products.append({
                "cve_id": cve_id,
                "name": "dotnet-runtime",  # Default - could be improved
                "min_vulnerable": f"{rel['release']}.0",  # Three-part version
                "max_vulnerable": rel["release_version"],
                "fixed": rel["release_version"],
                "release": rel["release"],
            })

    # Build the full structure
    result = {
        "last_updated": primary_date,
        "title": f".NET Security Updates for {year}/{month:02d}",
        "disclosures": disclosures,
        "products": products,
        "packages": [asdict(p) for p in all_packages] if all_packages else [],
    }

    # Add index structures
    result["product_cves"] = defaultdict(list)
    for p in products:
        if p["cve_id"] not in result["product_cves"][p["name"]]:
            result["product_cves"][p["name"]].append(p["cve_id"])
    result["product_cves"] = dict(result["product_cves"])

    result["package_cves"] = defaultdict(list)
    for p in all_packages:
        if p.cve_id not in result["package_cves"][p.name]:
            result["package_cves"][p.name].append(p.cve_id)
    result["package_cves"] = dict(result["package_cves"])

    result["release_cves"] = defaultdict(list)
    for cve_id, rels in cve_releases.items():
        for rel in rels:
            if cve_id not in result["release_cves"][rel["release"]]:
                result["release_cves"][rel["release"]].append(cve_id)
    result["release_cves"] = dict(result["release_cves"])

    result["cve_releases"] = {}
    for cve_id, rels in cve_releases.items():
        result["cve_releases"][cve_id] = sorted(set(rel["release"] for rel in rels))

    return result


def main():
    if len(sys.argv) < 3:
        print(f"Usage: {sys.argv[0]} YEAR MONTH [--write]")
        print(f"Example: {sys.argv[0]} 2023 10")
        print(f"Example: {sys.argv[0]} 2023 10 --write  # Write to timeline directory")
        sys.exit(1)

    year = int(sys.argv[1])
    month = int(sys.argv[2])
    write_file = "--write" in sys.argv

    result = generate_cve_json(year, month)
    if result:
        output = json.dumps(result, indent=2)
        if write_file:
            out_dir = TIMELINE_DIR / str(year) / f"{month:02d}"
            out_dir.mkdir(parents=True, exist_ok=True)
            out_file = out_dir / "cve.json"
            with open(out_file, "w") as f:
                f.write(output)
                f.write("\n")
            print(f"Wrote {out_file}", file=sys.stderr)
        else:
            print(output)


if __name__ == "__main__":
    main()
