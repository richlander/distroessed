#!/usr/bin/env bash

# Usage: ./find_exe_versions.sh [search_path]
# Default search_path is current directory if not provided

search_path="${1:-.}"

find "$search_path" -type f -name "*.csproj" | while read -r csproj; do
    if grep -q '<OutputType>Exe</OutputType>' "$csproj"; then
        grep -o '<TargetFramework>net[0-9]\+\.[0-9]\+</TargetFramework>' "$csproj" | \
        sed -E 's|<TargetFramework>net([0-9]+\.[0-9]+)</TargetFramework>|\1|'
    fi
done | sort -uVr