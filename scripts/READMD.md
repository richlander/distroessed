# Distroessed scripts

These scripts make running these tools easier, for in-support .NET versions.

The script assume running on Linux.

Workflow:

```bash
rich@lussier:~/git$ cd distroessed/src/
rich@lussier:~/git/distroessed/src$ dotnet publish
rich@lussier:~/git/distroessed/src$ cd ../scripts/
rich@lussier:~/git/distroessed/scripts$ ./link-binaries.sh
```

This workflow symbolically links the tools into the `tools` directory, enabling them to be used from the (`.gitignore`) directory or via the scripts (which assume the presence of the tools in that directory).

```bash
rich@lussier:~/git/distroessed/scripts$ ls ../tools/
DistroessedExceptional  LinuxPackagesMd  SupportedOsMd  distroessed
rich@lussier:~/git/distroessed/scripts$ ls
READMD.md  link-binaries.sh  update-os-package-md.sh  update-support-md.sh
```
