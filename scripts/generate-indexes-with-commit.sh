#!/usr/bin/env bash
# Two-step index generation with commit-specific links
# This script helps generate cache-busting index files

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check arguments
if [ $# -eq 0 ]; then
    echo "Usage: $0 <release-notes-directory> [commit-sha]"
    echo ""
    echo "Examples:"
    echo "  $0 ~/git/core-rich/release-notes              # Step 1: Generate with 'main' links"
    echo "  $0 ~/git/core-rich/release-notes abc123       # Step 2: Generate with commit-specific links"
    echo ""
    echo "Workflow:"
    echo "  1. Run without commit SHA to generate with 'main' links"
    echo "  2. Review and commit the changes"
    echo "  3. Run again with the commit SHA to generate commit-specific links"
    echo "  4. Review and commit again"
    echo ""
    echo "The second commit is what you share with LLMs to avoid caching."
    exit 1
fi

RELEASE_NOTES_DIR="$1"
COMMIT_SHA="$2"
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
TOOLS_DIR="$( cd "$SCRIPT_DIR/../src" && pwd )"

# Verify directories exist
if [ ! -d "$RELEASE_NOTES_DIR" ]; then
    echo -e "${RED}Error: Release notes directory not found: $RELEASE_NOTES_DIR${NC}"
    exit 1
fi

if [ ! -d "$TOOLS_DIR/ShipIndex" ] || [ ! -d "$TOOLS_DIR/VersionIndex" ]; then
    echo -e "${RED}Error: Tool directories not found in $TOOLS_DIR${NC}"
    exit 1
fi

# Change to release notes directory
cd "$RELEASE_NOTES_DIR"

# Check if we're in a git repository
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    echo -e "${RED}Error: $RELEASE_NOTES_DIR is not a git repository${NC}"
    exit 1
fi

if [ -z "$COMMIT_SHA" ]; then
    # Step 1: Generate with 'main' branch links
    echo -e "${GREEN}=== Step 1: Generating with 'main' branch links ===${NC}"
    echo ""
    
    echo "Running ShipIndex..."
    dotnet run --project "$TOOLS_DIR/ShipIndex" -- "$RELEASE_NOTES_DIR"
    echo ""
    
    echo "Running VersionIndex..."
    dotnet run --project "$TOOLS_DIR/VersionIndex" -- "$RELEASE_NOTES_DIR"
    echo ""
    
    # Check if there are changes
    if git diff --quiet && git diff --cached --quiet; then
        echo -e "${YELLOW}No changes generated.${NC}"
        exit 0
    fi
    
    echo -e "${GREEN}=== Step 1 Complete ===${NC}"
    echo ""
    echo -e "${BLUE}Next steps:${NC}"
    echo "  1. Review the changes: git status && git diff"
    echo "  2. Commit the changes: git add . && git commit -m 'Update release indexes'"
    echo "  3. Note the commit SHA: git rev-parse HEAD"
    echo "  4. Run this script again with the commit SHA:"
    echo "     $0 $RELEASE_NOTES_DIR <commit-sha>"
else
    # Step 2: Generate with commit-specific links
    echo -e "${GREEN}=== Step 2: Generating with commit-specific links ===${NC}"
    echo -e "Using commit: ${YELLOW}$COMMIT_SHA${NC}"
    echo ""
    
    echo "Running ShipIndex..."
    dotnet run --project "$TOOLS_DIR/ShipIndex" -- "$RELEASE_NOTES_DIR" --commit "$COMMIT_SHA"
    echo ""
    
    echo "Running VersionIndex..."
    dotnet run --project "$TOOLS_DIR/VersionIndex" -- "$RELEASE_NOTES_DIR" --commit "$COMMIT_SHA"
    echo ""
    
    # Check if there are changes
    if git diff --quiet && git diff --cached --quiet; then
        echo -e "${YELLOW}No changes generated.${NC}"
        exit 0
    fi
    
    echo -e "${GREEN}=== Step 2 Complete ===${NC}"
    echo ""
    echo -e "${BLUE}Next steps:${NC}"
    echo "  1. Review the changes: git status && git diff"
    echo "  2. Commit the changes: git add . && git commit -m 'Update indexes with commit-specific links'"
    echo "  3. Note the commit SHA: git rev-parse HEAD"
    echo "  4. Share this commit with LLMs for cache-busting access:"
    echo "     https://raw.githubusercontent.com/richlander/core/<commit-sha>/release-notes/archives/index.json"
fi
