#!/bin/bash

# Script to generate JSON schemas for HalJson object models
# Usage: ./scripts/generate-schemas.sh <target-directory>

set -e

if [ $# -eq 0 ]; then
    echo "Usage: $0 <target-directory>"
    echo "Example: $0 schemas"
    exit 1
fi

TARGET_DIR="$1"

echo "Generating JSON schemas..."

# Build the GenerateJsonSchemas tool
echo "Building GenerateJsonSchemas tool..."
dotnet build src/GenerateJsonSchemas/GenerateJsonSchemas.csproj -c Release

# Run the tool to generate schemas
echo "Running GenerateJsonSchemas tool..."
dotnet run --project src/GenerateJsonSchemas/GenerateJsonSchemas.csproj -c Release -- "$TARGET_DIR"

echo "Schema generation complete!"