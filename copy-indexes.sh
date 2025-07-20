#!/bin/bash

# Complete publishing script for release indexes
# Usage: ./publish-indexes.sh <target-path>
# Example: ./publish-indexes.sh /Users/rich/git/core-rich/release-notes

if [ $# -eq 0 ]; then
    echo "Usage: $0 <target-path>"
    echo "Example: $0 /Users/rich/git/core-rich/release-notes"
    exit 1
fi

TARGET_PATH="$1"
SCHEMAS_PATH="${TARGET_PATH}/schemas"

echo "Publishing indexes to: $TARGET_PATH"
echo "Publishing schemas to: $SCHEMAS_PATH"

# Ensure target directory exists
if [ ! -d "$TARGET_PATH" ]; then
    echo "Error: Target path does not exist: $TARGET_PATH"
    exit 1
fi

# Step 1: Validate required binaries exist or build if needed
REQUIRED_TOOLS=("GenerateJsonSchemas" "UpdateIndexes" "CveMarkdown")
ARTIFACTS_DIR="./artifacts"
TOOLS_DIR="./tools"

echo "Validating required binaries..."

# Check if artifacts directory exists
if [ ! -d "$ARTIFACTS_DIR" ]; then
    echo "Artifacts directory not found. Building and publishing solution..."
    ./publish.sh
    if [ $? -ne 0 ]; then
        echo "Error: Failed to build and publish solution"
        exit 1
    fi
else
    echo "Artifacts directory found. Checking for required tools..."
fi

# Ensure tools directory and symlinks exist
if [ ! -d "$TOOLS_DIR" ]; then
    echo "Tools directory not found. Creating symlinks..."
    cd scripts
    ./link-binaries.sh
    cd ..
fi

# Validate each required tool exists and is executable
for tool in "${REQUIRED_TOOLS[@]}"; do
    tool_path="$TOOLS_DIR/$tool"
    if [ ! -f "$tool_path" ] || [ ! -x "$tool_path" ]; then
        echo "Tool not found or not executable: $tool_path"
        echo "Building and publishing solution..."
        ./publish.sh
        if [ $? -ne 0 ]; then
            echo "Error: Failed to build and publish solution"
            exit 1
        fi
        echo "Creating symlinks..."
        cd scripts
        ./link-binaries.sh
        cd ..
        break
    fi
done

echo "All required binaries validated successfully."

# Step 2: Generate JSON schemas
echo "Generating JSON schemas..."
cd scripts
./generate-schemas.sh
if [ $? -ne 0 ]; then
    echo "Error: Failed to generate schemas"
    exit 1
fi

# Step 3: Publish HAL schemas to target/schemas
echo "Publishing HAL schemas..."
./publish-hal-schemas.sh "$SCHEMAS_PATH"
if [ $? -ne 0 ]; then
    echo "Error: Failed to publish HAL schemas"
    exit 1
fi
cd ..

# Step 4: Run UpdateIndexes with target path
echo "Running UpdateIndexes to generate release indexes..."
./tools/UpdateIndexes "$TARGET_PATH"
if [ $? -ne 0 ]; then
    echo "Error: Failed to run UpdateIndexes"
    exit 1
fi

# Step 5: Generate CVE markdown files
echo "Generating CVE markdown files..."
TARGET_PATH_ABS=$(realpath "$TARGET_PATH")
cd scripts
./generate-cve-markdown.sh "$TARGET_PATH_ABS"
if [ $? -ne 0 ]; then
    echo "Warning: Some CVE markdown files failed to generate (continuing anyway)"
fi
cd ..

echo "Successfully published indexes to: $TARGET_PATH"
echo "Successfully published schemas to: $SCHEMAS_PATH"