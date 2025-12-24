#!/usr/bin/env bash
# Generate all release index files using the published tools
# Run this after link-binaries.sh has created symlinks in tools/

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

usage() {
    echo "Usage: $0 <target-directory> [--url-root <url>]"
    echo ""
    echo "Arguments:"
    echo "  target-directory    Directory containing release-notes data (input and output)"
    echo "  --url-root <url>    Base URL root for generated links"
    echo "                      Example: https://raw.githubusercontent.com/dotnet/core/commit-sha"
    echo ""
    echo "Examples:"
    echo "  $0 ~/git/core/release-notes"
    echo "  $0 ~/git/core/release-notes --url-root https://raw.githubusercontent.com/dotnet/core/abc123"
    exit 1
}

# Parse arguments
if [ $# -eq 0 ]; then
    usage
fi

TARGET_DIR=""
URL_ROOT=""

while [ $# -gt 0 ]; do
    case "$1" in
        --url-root)
            if [ -z "$2" ]; then
                echo -e "${RED}Error: --url-root requires a value${NC}"
                exit 1
            fi
            URL_ROOT="$2"
            shift 2
            ;;
        -h|--help)
            usage
            ;;
        *)
            if [ -z "$TARGET_DIR" ]; then
                TARGET_DIR="$1"
            else
                echo -e "${RED}Error: Unexpected argument: $1${NC}"
                usage
            fi
            shift
            ;;
    esac
done

if [ -z "$TARGET_DIR" ]; then
    echo -e "${RED}Error: target-directory is required${NC}"
    usage
fi

# Resolve paths
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
TOOLS_DIR="$( cd "$SCRIPT_DIR/../tools" && pwd )"

# Verify target directory exists
if [ ! -d "$TARGET_DIR" ]; then
    echo -e "${RED}Error: Target directory not found: $TARGET_DIR${NC}"
    exit 1
fi

# Verify tools exist
for tool in VersionIndex ShipIndex LlmsIndex; do
    if [ ! -x "$TOOLS_DIR/$tool" ]; then
        echo -e "${RED}Error: Tool not found or not executable: $TOOLS_DIR/$tool${NC}"
        echo "Run link-binaries.sh first to create symlinks, or publish the tools."
        exit 1
    fi
done

# Build the arguments
ARGS="$TARGET_DIR"
if [ -n "$URL_ROOT" ]; then
    ARGS="$ARGS --url-root $URL_ROOT"
fi

echo -e "${GREEN}=== Generating Release Indexes ===${NC}"
echo "Target: $TARGET_DIR"
if [ -n "$URL_ROOT" ]; then
    echo "URL Root: $URL_ROOT"
fi
echo ""

# Run VersionIndex (generates index.json, {version}/index.json, manifests, etc.)
echo -e "${GREEN}Running VersionIndex...${NC}"
"$TOOLS_DIR/VersionIndex" $ARGS
echo ""

# Run ShipIndex (generates timeline/index.json, year/month indexes)
echo -e "${GREEN}Running ShipIndex...${NC}"
"$TOOLS_DIR/ShipIndex" $ARGS
echo ""

# Run LlmsIndex (generates llms.json)
echo -e "${GREEN}Running LlmsIndex...${NC}"
"$TOOLS_DIR/LlmsIndex" $ARGS
echo ""

# Run LlmsIndex (generates llms.json)
echo -e "${GREEN}Running LlmsIndex...${NC}"
"$TOOLS_DIR/LlmsIndex" $ARGS --workflows --output "$TARGET_DIR/llms2.json"
echo ""

echo -e "${GREEN}=== Complete ===${NC}"
