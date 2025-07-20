# .NET CVEs for 2024-08-13

The following vulnerabilities have been patched.

| ID                | Title             | Severity      | Product       | Platforms     | CVSS                         |
| ----------------- | ----------------- | ------------- | ------------- | ------------- | ---------------------------- |
| [CVE-2024-38167][CVE-2024-38167] | .NET Information disclosure vulnerability | High | .NET | All | CVSS:3.1/AV:N/AC:L/PR:N/UI:R/S:U/C:H/I:N/A:N/E:U/RL:O/RC:C |
| [CVE-2024-38168][CVE-2024-38168] | .NET Denial of Service Vulnerability | High | ASP.NET Core | windows | CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:U/C:N/I:N/A:H/E:U/RL:O/RC:C |


## Platform Components

The following table lists version ranges for affected platform components.

| Component     | Min Version   | Max Version | Fixed Version | CVE     | Source fix |
| ------------- | ------------- | --------- | --------- | ------------- | -------- |
| ASP.NET Runtime | >=8.0.0     | <=8.0.7   | [8.0.8](https://github.com/dotnet/core/blob/main/release-notes/8.0/8.0.8/8.0.8.md) | CVE-2024-38168 | [ffa0a02][ffa0a02]  |
| .NET Runtime  | >=8.0.0       | <=8.0.7   | [8.0.8](https://github.com/dotnet/core/blob/main/release-notes/8.0/8.0.8/8.0.8.md) | CVE-2024-38167 | [56cf645][56cf645]  |


| Component     | Min Version   | Max Version | Fixed Version | CVE         | Source fix |
| ------------- | ------------- | ----------- | ------------- | ----------- | ---------- |
| .NET          | 8.0.0         | 8.0.1       | 8.0.2         | CVE-BLAH    |            |

## Packages

The following table lists version ranges for affected packages.

| Package       | Min Version   | Max Version | Fixed Version | CVE     | Source fix |
| ------------- | ------------- | --------- | --------- | ------------- | -------- |
| [System.Security.Cryptography][System.Security.Cryptography] | >=8.0.0 | <=8.0.7 | [8.0.8](https://www.nuget.org/packages/System.Security.Cryptography/8.0.8) | CVE-2024-38167 | [56cf645][56cf645]  |



## Commits

The following table lists commits for affected packages.

| Repo                        | Branch            | Commit                                                   |
| --------------------------- | ----------------- | -------------------------------------------------------- |
| [dotnet/aspnetcore][dotnet/aspnetcore] | [release/8.0][release/8.0] | [ffa0a02][ffa0a02]                   |
| [dotnet/runtime][dotnet/runtime] | [release/8.0][release/8.0] | [56cf645][56cf645]                         |



[CVE-2024-38167]: https://github.com/dotnet/runtime/security/advisories
[CVE-2024-38168]: https://github.com/dotnet/aspnetcore/security/advisories
[System.Security.Cryptography]: https://www.nuget.org/packages/System.Security.Cryptography
[dotnet/aspnetcore]: https://github.com/dotnet/aspnetcore
[release/8.0]: https://github.com/dotnet/aspnetcore/tree/release/8.0
[ffa0a02]: https://github.com/dotnet/aspnetcore/commit/ffa0a028464e13d46aaec0c5ad8de0725a4d5aa5
[dotnet/runtime]: https://github.com/dotnet/runtime
[56cf645]: https://github.com/dotnet/runtime/commit/56cf645f455120395e5b62366921b21694510982
