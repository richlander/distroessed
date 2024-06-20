# distroessed

Managing distro versions is distressing.

We can join [.NET support information](https://github.com/dotnet/core/pull/9294) with [endOflife.date](https://endoflife.date/) to help us find head and tail releases that we should consider acting on.

This is what the tool produces as output.

```bash
$ dotnet run
**Android**
 Android
  Releases active : 4
  Unsupported active releases: 1
  Releases EOL soon: 0
  Supported inactive releases: 0
  Releases that are active but not supported:
  12.1

**Apple**
 iOS
  Releases active : 3
  Unsupported active releases: 0
  Releases EOL soon: 0
  Supported inactive releases: 0

 iPadOS
  Releases active : 3
  Unsupported active releases: 0
  Releases EOL soon: 0
  Supported inactive releases: 0

 macOS
  Releases active : 3
  Unsupported active releases: 0
  Releases EOL soon: 0
  Supported inactive releases: 0

**Linux**
 Alpine
  Releases active : 4
  Unsupported active releases: 1
  Releases EOL soon: 0
  Supported inactive releases: 0
  Releases that are active but not supported:
  3.20

 Debian
  Releases active : 2
  Unsupported active releases: 0
  Releases EOL soon: 1
  Supported inactive releases: 0
  Releases that are EOL within 2 months:
  11

 Fedora
  Releases active : 2
  Unsupported active releases: 0
  Releases EOL soon: 0
  Supported inactive releases: 0

 openSUSE Leap
  Releases active : 2
  Unsupported active releases: 1
  Releases EOL soon: 0
  Supported inactive releases: 0
  Releases that are active but not supported:
  15.6

 Red Hat Enterprise Linux
  Releases active : 3
  Unsupported active releases: 1
  Releases EOL soon: 1
  Supported inactive releases: 0
  Releases that are active but not supported:
  7
  Releases that are EOL within 2 months:
  7

 SUSE Enterprise Linux
  Releases active : 2
  Unsupported active releases: 2
  Releases EOL soon: 0
  Supported inactive releases: 0
  Releases that are active but not supported:
  15.5
  12.5

 Ubuntu
  Releases active : 4
  Unsupported active releases: 0
  Releases EOL soon: 1
  Supported inactive releases: 0
  Releases that are EOL within 2 months:
  23.10

**Windows**
 Windows
  Releases active : 11
  Unsupported active releases: 11
  Releases EOL soon: 0
  Supported inactive releases: 0
  Releases that are active but not supported:
  11-23h2-e
  11-23h2-w
  10-22h2
  11-22h2-e
  11-22h2-w
  10-21h2-iot-lts
  10-21h2-e-lts
  11-21h2-e
  10-1809-e-lts
  10-1607-e-lts
  10-1507-e-lts

 Windows Server
  Releases active : 4
  Unsupported active releases: 4
  Releases EOL soon: 0
  Supported inactive releases: 0
  Releases that are active but not supported:
  23H2
  2022
  2019
  2016
```
