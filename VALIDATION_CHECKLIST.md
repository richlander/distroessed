# .NET Code Changes Validation Checklist

## Pre-Build Validation

### 1. Compilation Check
```bash
cd src
dotnet build
```
**Expected**: All projects should compile without errors

### 2. Syntax Verification
Check for:
- Missing semicolons
- Incorrect attribute syntax
- Typos in method/property names
- Missing imports

### 3. Specific Changes to Verify

#### ReleaseStability.IsSupported Method
**File**: `src/DotnetRelease/DataModel/ReleaseInfo/ReleaseEnums.cs`
- [ ] Method compiles correctly
- [ ] DateTimeOffset parameter handling is correct
- [ ] Enum comparison logic works
- [ ] Null checking is appropriate

#### JSON Property Name Changes  
**Files**: Multiple `*.cs` files in `DataModel/HalJson/`
- [ ] `JsonPropertyName("_metadata")` syntax is correct
- [ ] No missing commas in attribute lists
- [ ] Consistent with existing patterns

#### Supported Property Addition
**File**: `src/DotnetRelease/DataModel/HalJson/ReleaseVersionIndex.cs`
- [ ] `bool? Supported` property compiles
- [ ] JsonIgnore attribute is correct
- [ ] Property placement doesn't break record syntax

#### ReleaseIndexFiles Changes
**File**: `src/UpdateIndexes/ReleaseIndexFiles.cs`
- [ ] Method signature change for `GetPatchIndexEntries` is correct
- [ ] All calls to updated method include new parameter
- [ ] `ReleaseStability.IsSupported()` calls compile
- [ ] MediaType.HalJson references are valid

## Runtime Testing

### 1. Generate Test Index
```bash
# Run UpdateIndexes on sample data
./tools/UpdateIndexes /path/to/test/data
```

### 2. Verify JSON Output Structure
Check generated `index.json` contains:
- [ ] Extended glossary with both cases
- [ ] `_metadata` instead of `metadata`  
- [ ] `supported` flags on release entries
- [ ] Consistent HAL+JSON content types
- [ ] Updated link descriptions

### 3. JSON Schema Validation
- [ ] Generated JSON validates against existing schemas
- [ ] No breaking changes to existing structure

## Integration Testing

### 1. Backwards Compatibility
- [ ] Existing consumers can still parse the JSON
- [ ] Optional fields don't break existing logic
- [ ] Schema compatibility maintained

### 2. Chat Assistant Testing
- [ ] Glossary lookups work for both cases
- [ ] Support status can be read directly
- [ ] HAL navigation works correctly

## Potential Issues to Watch For

### Compilation Errors
- Missing `using System.Text.Json.Serialization;`
- Incorrect attribute syntax
- Type mismatches in method calls
- Parameter count mismatches

### Runtime Errors  
- Null reference exceptions in `IsSupported`
- JSON serialization failures
- Schema validation failures
- Logic errors in support calculation

### Logic Issues
- Support flag calculation incorrect
- Content-type inconsistencies remain
- Glossary entries malformed
- HAL link navigation broken

## Success Criteria
- [ ] All projects compile successfully
- [ ] Generated JSON matches expected structure
- [ ] No existing functionality is broken
- [ ] All ChatGPT feedback items are addressed
- [ ] Chat assistants can consume improved data