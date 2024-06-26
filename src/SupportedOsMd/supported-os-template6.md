# .NET 6 - Supported OS versions

[.NET 6](README.md) is a [Long Term Support (LTS)](../../release-policies.md) release and [is supported](../../support.md) on multiple operating systems per their lifecycle policy.

This file is generated from [supported-os.json](supported-os.json) and is based on support information from [endoflife.date](https://endoflife.date/).

PLACEHOLDER-FAMILIES
## Linux compatibility

Microsoft-provided [portable](../../linux-support.md) Linux builds define [minimum compatibility](/linux-support.md) primarily via libc version.

PLACEHOLDER-LIBC

Note: Microsoft-provided portable Arm32 glibc builds are supported on distro versions with a [Y2038 incompatible glibc](https://github.com/dotnet/core/discussions/9285) or a Y2038 compatible glibc with [_TIME_BITS](https://www.gnu.org/software/libc/manual/html_node/Feature-Test-Macros.html) set to 32-bit, for example Debian 12, Ubuntu 22.04, and lower versions.

## Notes

PLACEHOLDER-NOTES

## Out of support OS versions

Support for the following operating system versions has ended.

PLACEHOLDER-UNSUPPORTED
