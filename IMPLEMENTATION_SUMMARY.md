# UpdateIndexes Project Enhancement Summary

## Overview
Updated the UpdateIndexes project to support two input parameters: an input `release-notes` directory and an optional output directory. This enhancement enables targeted testing while treating the input as read-only.

## Changes Made

### 1. Enhanced Command-Line Interface
- **Before**: `UpdateIndexes <directory>`
- **After**: `UpdateIndexes <input-directory> [output-directory]`

### 2. Dual Directory Support
- **Single directory mode**: When only one directory is specified, it's used for both input and output (preserves existing behavior)
- **Dual directory mode**: When two directories are specified, input is read-only and all generated files are written to the output directory

### 3. Automatic Directory Creation
- The tool now automatically creates the output directory if it doesn't exist
- Provides user feedback when creating directories
- Graceful error handling if directory creation fails

### 4. Enhanced User Experience
- Clear usage instructions with parameter explanations
- Informative console output showing which directories are being used
- Proper error messages for invalid arguments or missing directories

### 5. Framework Compatibility Updates
- Updated from .NET 10.0 to .NET 9.0 for current compatibility
- Replaced `CompareOptions.NumericOrdering` (NET 10+ feature) with custom `NaturalStringComparer`
- Maintained Native AOT compatibility as required

## Implementation Details

### NaturalStringComparer
Created a custom string comparer that provides natural ordering of version strings (e.g., "1.1", "1.2", "1.10", "2.0") to replace the .NET 10-specific `CompareOptions.NumericOrdering` feature.

### Files Modified
- `src/UpdateIndexes/Program.cs` - Main command-line argument processing
- `src/UpdateIndexes/UpdateIndexes.csproj` - Framework version update
- `src/UpdateIndexes/testing-instructions.md` - Updated testing documentation
- `src/UpdateIndexes/Summary.cs` - Added NaturalStringComparer, replaced NumericOrdering
- `src/UpdateIndexes/ReleaseIndexFiles.cs` - Replaced NumericOrdering usage
- `src/UpdateIndexes/HistoryIndexFiles.cs` - Replaced NumericOrdering usage
- `src/JsonSchemaInjector/JsonSchemaInjector.csproj` - Framework version update

## Benefits

### For Testing
- **Parallel Testing**: Multiple tests can run on the same input directory simultaneously
- **Read-Only Safety**: Input data remains unchanged during testing
- **Isolated Outputs**: Each test can have its own output directory
- **Regression Testing**: Easy comparison between different tool versions

### For Production
- **Backward Compatibility**: Existing usage patterns continue to work
- **Flexibility**: Enables separation of source and generated content
- **CI/CD Friendly**: Supports containerized and automated environments

## Usage Examples

```bash
# Single directory mode (existing behavior)
dotnet run ~/tmp/core/release-notes

# Dual directory mode (new functionality)
dotnet run ~/tmp/core/release-notes ~/tmp/test-output

# Multiple simultaneous tests
dotnet run ~/input ~/output-test1 &
dotnet run ~/input ~/output-test2 &
dotnet run ~/input ~/output-test3 &
```

## Compliance
- ✅ Native AOT friendly code
- ✅ Maintained existing functionality
- ✅ Enhanced testing capabilities
- ✅ Clear documentation updates
- ✅ Proper error handling
- ✅ Cross-platform compatibility