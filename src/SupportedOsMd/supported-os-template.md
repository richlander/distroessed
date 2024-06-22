# .NET 9 - Supported OS versions

[.NET 9](README.md) is a [Standard Term Support (STS)](../../release-policies.md) release and [is supported](../../support.md) on multiple operating systems per their lifecycle policy.

PLACEHOLDER-FAMILIES
## Linux compatibility

Microsoft-provided portable Linux builds define [minimum compatibility](/linux-support.md) primarily via libc version.

PLACEHOLDER-LIBC

Note: Arm32 builds are supported on distro versions with a [Y2038 compatible glibc](https://github.com/dotnet/core/discussions/9285), for example Debian 12, Ubuntu 22.04, and higher versions.

## QEMU compatibility

The [QEMU](https://www.qemu.org/) emulator is not supported to run .NET apps. QEMU is used, for example, to emulate Arm64 containers on x64, and vice versa.

## Out of support OS versions

Support for the following versions was ended by the distribution owners and are [no longer supported by .NET 9.0][OS-lifecycle-policy].

[OS-lifecycle-policy]: https://github.com/dotnet/core/blob/main/os-lifecycle-policy.md
