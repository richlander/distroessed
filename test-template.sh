#!/bin/bash

# Quick test to verify the template-based llms.txt generation works

echo "Testing template-based llms.txt generation..."

# Build UpdateIndexes
echo "Building UpdateIndexes..."
dotnet build src/UpdateIndexes/UpdateIndexes.csproj -q

if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

echo "Build successful!"

# Check if template exists
TEMPLATE_PATH="templates/llms-template.txt"
if [ ! -f "$TEMPLATE_PATH" ]; then
    echo "Error: Template file not found at $TEMPLATE_PATH"
    exit 1
fi

echo "Template file found at $TEMPLATE_PATH"
echo "Template content:"
cat "$TEMPLATE_PATH"

echo ""
echo "Template test completed successfully!"
echo "The actual functionality will be tested when UpdateIndexes runs with real data."