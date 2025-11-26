#!/usr/bin/env python3
"""
Fetch CVE data from GitHub announcements and generate cve.json files.

This is a one-time script to backfill historical CVE data.
"""

import json
import re
import sys
import time
from dataclasses import dataclass
from pathlib import Path
from urllib.request import urlopen, Request
from urllib.error import HTTPError


@dataclass
class PackageInfo:
    name: str
    min_vulnerable: str
    max_vulnerable: str
    fixed: str
    release: str  # e.g., "6.0", "7.0"


def fetch_github_issue(issue_number: int) -> dict | None:
    """Fetch a GitHub issue by number."""
    url = f"https://api.github.com/repos/dotnet/announcements/issues/{issue_number}"
    req = Request(url, headers={"User-Agent": "cve-fetcher"})
    try:
        with urlopen(req) as response:
            return json.loads(response.read())
    except HTTPError as e:
        print(f"  Error fetching issue {issue_number}: {e}")
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
        print(f"  Error searching for {cve_id}: {e}")
        return []


def parse_version_range(version_str: str) -> tuple[str, str]:
    """Parse '>= 6.0.0, <= 6.0.21' into (min, max)."""
    version_str = version_str.strip()

    # Handle various formats
    # ">= 6.0.0, <= 6.0.21"
    # ">=6.0.0, <= 6.0.21" (no space)
    # ">= 6.0.0, < 6.0.22" (less than instead of <=)

    min_ver = ""
    max_ver = ""

    parts = version_str.split(",")
    for part in parts:
        part = part.strip()
        if ">=" in part:
            min_ver = part.replace(">=", "").strip()
        elif "<=" in part:
            max_ver = part.replace("<=", "").strip()
        elif "<" in part:
            # "< 6.0.22" means max is the version before
            max_ver = part.replace("<", "").strip()

    return min_ver, max_ver


def extract_release_from_version(version: str) -> str:
    """Extract release (e.g., '6.0') from version (e.g., '6.0.21')."""
    parts = version.split(".")
    if len(parts) >= 2:
        return f"{parts[0]}.{parts[1]}"
    return version


def parse_packages_from_body(body: str, cve_id: str) -> list[PackageInfo]:
    """Parse package tables from announcement body."""
    packages = []

    # Match lines like:
    # [System.Text.Json](url) | >= 6.0.0, <= 6.0.9 | 6.0.10
    # [Microsoft.NETCore.App.Runtime.linux-x64](url)|>= 7.0.0, <= 7.0.10 |7.0.11
    pattern = r'\[([^\]]+)\]\([^)]+\)\s*\|\s*([^|]+)\s*\|\s*(\S+)'

    for match in re.finditer(pattern, body):
        pkg_name = match.group(1).strip()
        version_range = match.group(2).strip()
        fixed = match.group(3).strip()

        # Skip platform-specific runtime packages
        if pkg_name.startswith("Microsoft.NETCore.App.Runtime."):
            continue
        if pkg_name.startswith("Microsoft.AspNetCore.App.Runtime."):
            continue
        if pkg_name.startswith("Microsoft.WindowsDesktop.App.Runtime."):
            continue

        min_ver, max_ver = parse_version_range(version_range)
        release = extract_release_from_version(fixed)

        packages.append(PackageInfo(
            name=pkg_name,
            min_vulnerable=min_ver,
            max_vulnerable=max_ver,
            fixed=fixed,
            release=release
        ))

    return packages


def extract_description(body: str) -> list[str]:
    """Extract the vulnerability description from announcement body."""
    # Normalize line endings
    body = body.replace('\r\n', '\n').replace('\r', '\n')

    # Remove the boilerplate "Microsoft is releasing..." sentence
    def clean_description(text: str) -> str:
        text = re.sub(r'\[([^\]]+)\]\([^)]+\)', r'\1', text)  # Remove markdown links
        # Remove boilerplate
        text = re.sub(
            r'Microsoft is releasing this security advisory[^.]*\.',
            '',
            text,
            flags=re.IGNORECASE
        )
        text = re.sub(
            r'This advisory also provides guidance[^.]*\.',
            '',
            text,
            flags=re.IGNORECASE
        )
        return text.strip()

    # Look for executive summary section - try multiple patterns
    patterns = [
        r'#{2,3}\s*.*?Executive [Ss]ummary.*?\n\n(.*?)(?=\n#{2,3})',  # ## or ### Executive Summary
        r'#{2,3}\s*.*?Executive [Ss]ummary.*?\n(.*?)(?=\n#{2,3})',
    ]

    for pattern in patterns:
        match = re.search(pattern, body, re.IGNORECASE | re.DOTALL)
        if match:
            desc = clean_description(match.group(1))
            if desc:
                # Get the actual vulnerability description (usually second paragraph)
                paragraphs = [p.strip() for p in desc.split('\n\n') if p.strip()]
                # Filter out very short fragments (like leftover component names)
                paragraphs = [p for p in paragraphs if len(p) > 50]
                for p in paragraphs:
                    if 'vulnerability' in p.lower() or 'susceptible' in p.lower() or 'exists' in p.lower():
                        return [p]
                if paragraphs:
                    return [paragraphs[0]]

    # Try looking for description after "Executive Summary" heading
    match = re.search(r'Executive [Ss]ummary\s*\n+(.*?)(?=\n#|\n\*\*)', body, re.DOTALL)
    if match:
        desc = clean_description(match.group(1))
        paragraphs = [p.strip() for p in desc.split('\n\n') if p.strip()]
        for p in paragraphs:
            if 'vulnerability' in p.lower():
                return [p]
        if paragraphs:
            return [paragraphs[-1]]  # Last paragraph often has the actual description

    # Old format (2017-2018): Look for description after title, before ### headings
    match = re.search(r'# Microsoft Security Advisory.*?\n+(.*?)(?=\n###|\n##)', body, re.DOTALL)
    if match:
        desc = clean_description(match.group(1))
        if desc:
            # Find the substantive description paragraph
            paragraphs = [p.strip() for p in desc.split('\n\n') if p.strip()]
            for p in paragraphs:
                if 'vulnerability' in p.lower() or 'aware of' in p.lower():
                    return [p]
            if paragraphs:
                return [paragraphs[0]]

    return []


def extract_problem_type(body: str, title: str) -> str:
    """Extract the problem/vulnerability type."""
    # Common patterns in titles
    patterns = [
        r'(Denial of Service)',
        r'(Remote Code Execution)',
        r'(Elevation of Privilege)',
        r'(Information Disclosure)',
        r'(Security Feature Bypass)',
        r'(Spoofing)',
        r'(Tampering)',
    ]

    for pattern in patterns:
        if re.search(pattern, title, re.IGNORECASE):
            return re.search(pattern, title, re.IGNORECASE).group(1)
        if re.search(pattern, body, re.IGNORECASE):
            return re.search(pattern, body, re.IGNORECASE).group(1)

    return "Security Vulnerability"


def extract_platforms(body: str) -> list[str]:
    """Extract affected platforms from announcement."""
    body_lower = body.lower()

    if "only affects linux" in body_lower or "linux systems" in body_lower:
        return ["linux"]
    if "only affects windows" in body_lower or "windows systems" in body_lower:
        return ["windows"]
    if "only affects macos" in body_lower or "macos systems" in body_lower:
        return ["macos"]

    return ["all"]


def get_cve_data(cve_id: str) -> dict | None:
    """Fetch and parse CVE data from GitHub announcement."""
    print(f"  Fetching {cve_id}...")

    # Search for the issue
    issue_numbers = search_cve_issues(cve_id)
    time.sleep(0.5)  # Rate limiting

    if not issue_numbers:
        print(f"    No announcement found for {cve_id}")
        return None

    # Find the canonical issue - prefer issues with CVE ID in title (actual advisory)
    # over monthly summary issues that just mention the CVE
    for issue_num in issue_numbers:
        issue = fetch_github_issue(issue_num)
        time.sleep(0.5)  # Rate limiting

        if not issue:
            continue

        body = issue.get("body", "")
        title = issue.get("title", "")

        # Skip if CVE not in title - these are usually monthly summaries
        if cve_id not in title:
            continue

        packages = parse_packages_from_body(body, cve_id)
        description = extract_description(body)
        problem = extract_problem_type(body, title)
        platforms = extract_platforms(body)

        return {
            "cve_id": cve_id,
            "issue_number": issue_num,
            "title": title,
            "problem": problem,
            "description": description,
            "platforms": platforms,
            "packages": packages,
            "announcement_url": f"https://github.com/dotnet/announcements/issues/{issue_num}"
        }

    return None


def main():
    # Test with a few CVEs
    test_cves = [
        "CVE-2024-43483",  # Has System.Text.Json packages
        "CVE-2023-36799",  # Linux-only, runtime packages only
        "CVE-2018-0764",   # Old format, no package table
    ]

    for cve_id in test_cves:
        print(f"\n=== {cve_id} ===")
        data = get_cve_data(cve_id)
        if data:
            print(f"  Problem: {data['problem']}")
            print(f"  Platforms: {data['platforms']}")
            desc = data['description']
            if desc:
                print(f"  Description: {desc[0][:100]}...")
            else:
                print(f"  Description: N/A")
            print(f"  Packages ({len(data['packages'])}):")
            for pkg in data['packages'][:3]:
                print(f"    - {pkg.name}: {pkg.min_vulnerable} -> {pkg.fixed}")
            if len(data['packages']) > 3:
                print(f"    ... and {len(data['packages']) - 3} more")
        print()


if __name__ == "__main__":
    main()
