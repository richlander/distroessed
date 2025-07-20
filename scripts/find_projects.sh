#!/usr/bin/env bash

# Usage: ./find_projects.sh [search_path]
# Default search_path is current directory if not provided

search_path="${1:-.}"

# Print header
echo "OutputType,TargetFramework,Location"

# Find all .csproj files in the specified directory tree
find "$search_path" -type f -name "*.csproj" | while read -r csproj; do
    # Extract OutputType (first match) or default to "classlib" if not found
    output_type=$(grep -m1 '<OutputType>' "$csproj" | sed -E 's|.*<OutputType>(.*)</OutputType>.*|\1|')
    if [[ -z "$output_type" ]]; then
        output_type="classlib"
    fi
    # Extract TargetFramework (first match)
    target_framework=$(grep -m1 '<TargetFramework>' "$csproj" | sed -E 's|.*<TargetFramework>(.*)</TargetFramework>.*|\1|')
    # Print row if TargetFramework was found
    if [[ -n "$target_framework" ]]; then
        echo "$output_type,$target_framework,$csproj"
    fi
done