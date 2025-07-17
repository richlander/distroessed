#!/bin/bash
set -e

# Script to publish HAL JSON schemas that are referenced by UpdateIndexes to a target location
# Usage: ./publish-hal-schemas.sh <target_directory>
#
# This script copies the specific schema files that are referenced in the UpdateIndexes code
# to a target directory, which is typically a release-notes/schemas directory in a Git repository.

# Check if target directory is provided
if [ $# -eq 0 ]; then
    echo "Usage: $0 <target_directory>"
    echo "Example: $0 /path/to/release-notes/schemas"
    exit 1
fi

TARGET_DIR="$1"

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
ROOT_DIR="$( cd "$SCRIPT_DIR/.." && pwd )"
SCHEMAS_DIR="$ROOT_DIR/schemas"

# Schema files referenced by UpdateIndexes
HAL_SCHEMAS=(
    "dotnet-release-version-index.json"
    "dotnet-release-history-index.json"
    "dotnet-release-manifest.json"
)

# Create target directory if it doesn't exist
mkdir -p "$TARGET_DIR"

echo "Publishing HAL JSON schemas to: $TARGET_DIR"

# Copy each schema file
for schema in "${HAL_SCHEMAS[@]}"; do
    source_file="$SCHEMAS_DIR/$schema"
    target_file="$TARGET_DIR/$schema"
    
    if [ -f "$source_file" ]; then
        cp "$source_file" "$target_file"
        echo "Copied: $schema"
    else
        echo "Warning: Schema file not found: $source_file"
        echo "Make sure to run generate-schemas.sh first to create the schema files."
    fi
done

echo "HAL schema publishing completed!"
echo
echo "Published schemas:"
for schema in "${HAL_SCHEMAS[@]}"; do
    target_file="$TARGET_DIR/$schema"
    if [ -f "$target_file" ]; then
        echo "  ✓ $schema"
    else
        echo "  ✗ $schema (missing)"
    fi
done