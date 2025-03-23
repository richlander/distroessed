# .NET CVEs for 2024-08-13

The following vulnerabilities were disclosed this month.

| CVE           | Description       | Product       | Platforms     | CVSS             |
| ------------- | ----------------- | ------------- | ------------- | ---------------- |
| [CVE-2024-38167][CVE-2024-38167] | .NET Information disclosure vulnerability | .NET | all | CVSS:3.1/AV:N/AC:L/PR:N/UI:R/S:U/C:H/I:N/A:N/E:U/RL:O/RC:C |
| [CVE-2024-38168][CVE-2024-38168] | .NET Denial of Service Vulnerability | ASP.NET Core | windows | CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:U/C:N/I:N/A:H/E:U/RL:O/RC:C |

## Vulnerable and patched packages

The following table lists vulnerable and patched version ranges for affected packages.

| CVE           | Package       | Min Version | Max Version | Fixed Version |
| ------------- | ------------- | --------- | --------- | ------------ |
| [CVE-2024-38167][CVE-2024-38167] | Microsoft.NETCore.App.Runtime‑\* | >=8.0.0 | <=8.0.7 | 8.0.8 |
| [CVE-2024-38168][CVE-2024-38168] | Microsoft.AspNetCore.App.Runtime‑\* | >=8.0.0 | <=8.0.7 | 8.0.8 |

## Commits

The following table lists commits for affected packages.

| CVE                         | Branch            | Commit                                                   |
| --------------------------- | ----------------- | -------------------------------------------------------- |
| [CVE-2024-38167][CVE-2024-38167] | [release/8.0](https://github.com/dotnet/runtime/tree/release/8.0) | [56cf645f455120395e5b62366921b21694510982](https://github.com/dotnet/runtime/commit/56cf645f455120395e5b62366921b21694510982) |
| [CVE-2024-38168][CVE-2024-38168] | [release/8.0](https://github.com/dotnet/aspnetcore/tree/release/8.0) | [b77fef78f17bbbce69d5aeacf2fcdbb8cb408b98](https://github.com/dotnet/aspnetcore/commit/b77fef78f17bbbce69d5aeacf2fcdbb8cb408b98) |

[CVE-2024-38167]: https://www.cve.org/CVERecord?id=CVE-2024-38167
[CVE-2024-38168]: https://www.cve.org/CVERecord?id=CVE-2024-38168
