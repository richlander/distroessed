#!/usr/bin/env bash

# Usage: ./find_referenced_versions.sh [search_path]
# Default search_path is current directory if not provided

search_path="${1:-.}"

find "$search_path" -type f -name "*.csproj" | while read -r csproj; do
    grep -o '<TargetFramework>net[0-9]\+\.[0-9]\+</TargetFramework>' "$csproj" | \
    sed -E 's|<TargetFramework>net([0-9]+\.[0-9]+)</TargetFramework>|\1|'
done | sort -uVr