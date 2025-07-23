# UpdateIndexes Refactoring Notes

## Current Architecture Issues

### 1. Complex Link Ordering Logic
- Current approach generates all links then reorders them, which is fragile and hard to follow
- The reordering logic in `ReleaseIndexFiles.cs:299-350` is complex and error-prone
- Post-generation filtering by MediaType is a code smell

### 2. HalLinkGenerator Assumptions
- Assumes the first file in the collection is always "self", which breaks when filtering/reordering
- The `isSelf` flag logic in `HalLinkGenerator.cs:42-46` is brittle
- Special case handling for "history/index.json" is hardcoded

### 3. Dictionary Key Naming Inconsistencies
- Relationship between `MainFileMappings` keys, filenames, and final link property names is confusing
- Property name derivation logic is scattered and unpredictable
- Markdown files get auto-generated suffixes (`-markdown-raw`, `-markdown`) that may not match spec requirements

### 4. No Clear Separation of Concerns
- Link generation, ordering, and dynamic link insertion are all mixed together in one large method
- Business logic for finding latest releases is embedded in link generation code
- File existence checking, URL generation, and property naming are coupled

### 5. Difficult to Test
- Current structure makes it hard to unit test individual pieces
- No tests exist for link ordering, property naming, or dynamic link generation
- Hard to verify spec compliance without running the entire tool

## Proposed Better Architecture

### Link Builder Pattern
```csharp
public interface ILinkBuilder
{
    Dictionary<string, HalLink> BuildLinks(string basePath, IEnumerable<ReleaseVersionIndexEntry> releases);
    int Priority { get; } // For ordering
}

public class StaticHalJsonLinkBuilder : ILinkBuilder
public class DynamicHalJsonLinkBuilder : ILinkBuilder  
public class MarkdownLinkBuilder : ILinkBuilder
```

### Clear Ordering Strategy
- Each link builder has a priority
- Links are built in priority order, eliminating need for post-generation reordering
- Builders are responsible for their own property naming conventions

### Predictable Key Naming
```csharp
public class LinkKeyMapper
{
    public string GetPropertyName(string filename, LinkType type, LinkStyle style);
    // e.g., GetPropertyName("usage.md", LinkType.Markdown, LinkStyle.Raw) → "usage"
}
```

### Separation of Concerns
```csharp
public class RootIndexGenerator
{
    private readonly IEnumerable<ILinkBuilder> _linkBuilders;
    private readonly IReleaseResolver _releaseResolver;
    private readonly ILinkKeyMapper _keyMapper;
    
    public ReleaseVersionIndex Generate(string inputPath);
}
```

### Unit Testing Structure
```csharp
[Test] public void StaticHalJsonLinks_FollowSpecOrder()
[Test] public void DynamicLinks_InsertedInCorrectPosition()  
[Test] public void MarkdownLinks_UseCorrectPropertyNames()
[Test] public void SelfLink_AlwaysPointsToIndexJson()
[Test] public void LinkOrdering_MatchesSpecification()
```

## Specific Issues to Address

### ReleaseIndexFiles.cs
- **Line 29-35**: `MainFileMappings` should be replaced with dedicated link builders
- **Line 293-350**: Complex reordering logic should be eliminated
- **Line 294-297**: HalLinkGenerator usage should be simplified

### HalLinkGenerator.cs
- **Line 42-46**: `isSelf` logic should be externalized
- **Line 34-38**: Special case handling should be configurable
- **Line 53**: Property name generation should be delegated to a mapper

## Migration Strategy

1. **Phase 1**: Create interfaces and base implementations without changing existing behavior
2. **Phase 2**: Add comprehensive unit tests for current behavior
3. **Phase 3**: Implement new link builders alongside existing code
4. **Phase 4**: Switch to new architecture and remove old code
5. **Phase 5**: Add integration tests to verify spec compliance

## Success Criteria

- [ ] Link ordering is deterministic and matches spec exactly
- [ ] Property names are predictable and configurable
- [ ] Each link type can be tested independently
- [ ] Adding new link types requires minimal code changes
- [ ] Spec compliance can be verified automatically
- [ ] Code is maintainable and easy to understand

---

*Note: These issues were identified during implementation of root-level cross-referencing in January 2025. The current implementation works but needs architectural improvements for long-term maintainability.*