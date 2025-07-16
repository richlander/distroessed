#!/bin/bash
set -e

# Script to generate JSON schemas using the GenerateJsonSchemas tool
# Usage: ./generate-schemas.sh [target_directory]
# 
# Prerequisites: This script assumes that link-binaries.sh has been run to create
# symlinks to the compiled binaries in the tools directory.

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
ROOT_DIR="$( cd "$SCRIPT_DIR/.." && pwd )"
TOOLS_DIR="$ROOT_DIR/tools"
GENERATE_SCHEMAS_TOOL="$TOOLS_DIR/GenerateJsonSchemas"

# Default target directory if not specified
TARGET_DIR="${1:-$ROOT_DIR/schemas}"

# Check if the GenerateJsonSchemas tool exists
if [ ! -f "$GENERATE_SCHEMAS_TOOL" ]; then
    echo "Error: GenerateJsonSchemas tool not found at $GENERATE_SCHEMAS_TOOL"
    echo "Please run link-binaries.sh first to create the necessary symlinks."
    exit 1
fi

# Create target directory if it doesn't exist
mkdir -p "$TARGET_DIR"

echo "Generating JSON schemas to: $TARGET_DIR"

# Run the GenerateJsonSchemas tool with the target directory
"$GENERATE_SCHEMAS_TOOL" "$TARGET_DIR"

echo "Schema generation completed successfully!"