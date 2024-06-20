# distroessed

Managing distro versions is distressing.

We can join [.NET support information](https://github.com/dotnet/core/pull/9294) with [endOflife.date](https://endoflife.date/) to help us find head and tail releases that we should consider acting on.

This is what the tool produces as output.

```bash
$ dotnet run
**Android**
 Android
  Releases active : 4
  Unsupported active releases: 0
  Releases EOL soon: 0
  Supported inactive releases: 0

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

 tvOS
  No data found at endoflife.date

**Linux**
 Alpine
  Releases active : 4
  Unsupported active releases: 0
  Releases EOL soon: 0
  Supported inactive releases: 0

 Debian
  Releases active : 2
  Unsupported active releases: 1
  Releases EOL soon: 1
  Supported inactive releases: 0
  Releases that are active but not supported:
  11
  Releases that are EOL within 2 months:
  11

 Fedora
  Releases active : 2
  Unsupported active releases: 0
  Releases EOL soon: 0
  Supported inactive releases: 0

 openSUSE Leap
  Releases active : 2
  Unsupported active releases: 0
  Releases EOL soon: 0
  Supported inactive releases: 0

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
  Unsupported active releases: 1
  Releases EOL soon: 1
  Supported inactive releases: 0
  Releases that are active but not supported:
  23.10
  Releases that are EOL within 2 months:
  23.10

**Windows**
 Nano Server
  No data found at endoflife.date

 Windows
  Releases active : 11
  Unsupported active releases: 3
  Releases EOL soon: 0
  Supported inactive releases: 0
  Releases that are active but not supported:
  10-21h2-iot-lts
  11-21h2-e
  10-1507-e-lts

 Windows Server
  Releases active : 4
  Unsupported active releases: 0
  Releases EOL soon: 0
  Supported inactive releases: 0

 Windows Server Core
  No data found at endoflife.date
```
