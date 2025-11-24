# Pull Request #1856 Summary: Obsolete SqlSaga Base Class

## Overview
This PR marks the `SqlSaga<TSagaData>` base class as obsolete and migrates the entire codebase to use the standard NServiceBus `Saga<TSagaData>` base class with the new saga mapping API.

**PR Details:**
- **Title:** Obsolete SqlSaga base class
- **Status:** Open
- **Author:** @andreasohlund
- **Created:** 2025-11-24
- **Branch:** `obsolete-sqlsaga`
- **Changes:** 56 files changed, 369 additions, 1,873 deletions
- **Commits:** 9

## Key Changes

### 1. API Obsolescence
The `SqlSaga<TSagaData>` base class and related interfaces are now marked as obsolete:

```csharp
[Obsolete("SqlSaga is no longer supported, use the normal sagas with the new mapping API.. Will be removed in version 10.0.0.", true)]
public abstract class SqlSaga<TSagaData>;

[Obsolete("SqlSaga is no longer supported, use the normal sagas with the new mapping API.. Will be removed in version 10.0.0.", true)]
public interface IMessagePropertyMapper
{
    void ConfigureMapping<TMessage>(Expression<Func<TMessage, object>> messageProperty);
}
```

### 2. Migration from Old API to New API

#### Old SqlSaga API (Obsolete):
```csharp
public class MySaga : SqlSaga<MySaga.SagaData>, IAmStartedByMessages<StartMessage>
{
    protected override string CorrelationPropertyName => nameof(SagaData.OrderId);
    protected override string TransitionalCorrelationPropertyName => nameof(SagaData.OldOrderId);
    protected override string TableSuffix => "MyCustomTableName";
    
    protected override void ConfigureMapping(IMessagePropertyMapper mapper)
    {
        mapper.ConfigureMapping<StartMessage>(m => m.OrderId);
    }
    
    public class SagaData : ContainSagaData
    {
        public string OrderId { get; set; }
        public string OldOrderId { get; set; }
    }
}
```

#### New Saga API (Recommended):
```csharp
[SqlSaga(
    tableSuffix: "MyCustomTableName",
    transitionalCorrelationProperty: nameof(SagaData.OldOrderId)
)]
public class MySaga : Saga<MySaga.SagaData>, IAmStartedByMessages<StartMessage>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
    {
        mapper.MapSaga(s => s.OrderId).ToMessage<StartMessage>(m => m.OrderId);
    }
    
    public class SagaData : ContainSagaData
    {
        public string OrderId { get; set; }
        public string OldOrderId { get; set; }
    }
}
```

### 3. Custom Finder Support

#### Old API:
```csharp
public class MySaga : SqlSaga<MySaga.SagaData>
{
    protected override string CorrelationPropertyName => null; // null when using custom finders
    protected override void ConfigureMapping(IMessagePropertyMapper mapper)
    {
        // No mapping when using custom finders
    }
}
```

#### New API:
```csharp
public class MySaga : Saga<MySaga.SagaData>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
    {
        mapper.ConfigureFinderMapping<MyMessage, MyCustomFinder>();
    }
}

public class MyCustomFinder : ISagaFinder<MySaga.SagaData, MyMessage>
{
    public Task<MySaga.SagaData> FindBy(MyMessage message, 
        ISynchronizedStorageSession session, 
        IReadOnlyContextBag context, 
        CancellationToken cancellationToken = default)
    {
        // Custom finder logic
    }
}
```

### 4. Key API Changes

| Old API | New API |
|---------|---------|
| `SqlSaga<T>` base class | `Saga<T>` base class |
| `CorrelationPropertyName` property | Part of `ConfigureHowToFindSaga` |
| `TransitionalCorrelationPropertyName` property | `[SqlSaga(transitionalCorrelationProperty: ...)]` attribute |
| `TableSuffix` property | `[SqlSaga(tableSuffix: ...)]` attribute |
| `ConfigureMapping(IMessagePropertyMapper)` | `ConfigureHowToFindSaga(SagaPropertyMapper<T>)` |
| `mapper.ConfigureMapping<TMessage>(...)` | `mapper.MapSaga(...).ToMessage<TMessage>(...)` |
| N/A | `mapper.ConfigureFinderMapping<TMessage, TFinder>()` |

### 5. Updated Dependencies
- **NServiceBus:** Updated from `10.0.0-alpha.16` to `10.0.0-alpha.17`
- **NServiceBus.AcceptanceTests.Sources:** Updated to alpha.17
- **NServiceBus.PersistenceTests.Sources:** Updated to alpha.17
- **NServiceBus.AcceptanceTesting:** Updated to alpha.17

### 6. File Changes Summary

#### Modified Core Files:
- `src/SqlPersistence/Saga/SqlSaga.cs` - Removed implementation
- `src/SqlPersistence/Saga/IMessagePropertyMapper.cs` - Removed implementation
- `src/SqlPersistence/Saga/PropertyMapper.cs` - Removed (obsolete)
- `src/SqlPersistence/obsoletes-v9.cs` - Added obsolete markers
- `src/SqlPersistence/SqlPersistence.csproj` - Updated NServiceBus dependency

#### Updated Test Helper Files:
- `src/AcceptanceTestHelper/ConfigureHowToFindSagaWithMessage.cs` - Added finder support
- `src/AcceptanceTestHelper/RuntimeSagaDefinitionReader.cs` - Removed SqlSaga handling

#### Modified ScriptBuilder Files:
- `src/ScriptBuilder/Saga/SagaDefinitionReader.cs` - Removed SqlSaga support
- `src/ScriptBuilder/Saga/InstructionAnalyzer.cs` - Added finder mapping detection
- Multiple writer and utility files with code cleanup

#### Test Files Updated (50+ files):
All acceptance test files across multiple databases (MS SQL, MySQL, PostgreSQL, Oracle) were updated to use the new API, including:
- Custom finder tests
- Transitional correlation property tests
- Transport integration tests
- Saga invocation tests

#### Removed Files:
- `When_outbox_disabled_and_different_persistence_is_used_for_sagas.cs` - Test removed (unsupported scenario)
- `When_correlation_property_is_not_mapped.cs` - Test removed (validation changed)
- `EnsureSqlSagaNotDecoratedBySqlSagaAttribute.cs` - Test no longer relevant
- `SqlSagaTests.cs` - Tests for obsolete API
- `MessagePropertyMapperTests.cs` - Tests for obsolete API
- `CharArrayTextWriterPerformanceTests.cs` - Performance test removed

### 7. Code Style Improvements
The PR also includes modernization of C# code style:
- Primary constructors: `public class Handler(Context context) : IHandleMessages<Message>`
- Expression-bodied members: `public Task Handle(...) => Task.CompletedTask;`
- Collection expressions: `return [];` instead of `return Enumerable.Empty<T>();`
- `using` declarations instead of `using` blocks
- Target-typed `new` expressions

### 8. Breaking Changes
⚠️ **This is a breaking change:**
- The `SqlSaga<T>` base class is marked obsolete with `error` severity
- Users must migrate to `Saga<T>` with the new mapping API
- Old saga configuration properties are no longer supported
- Custom finders must be configured differently

### 9. Migration Guide

**Step 1:** Change base class
```diff
- public class MySaga : SqlSaga<MySaga.SagaData>
+ public class MySaga : Saga<MySaga.SagaData>
```

**Step 2:** Add SqlSaga attribute if needed
```csharp
[SqlSaga(tableSuffix: "MyTable", transitionalCorrelationProperty: nameof(SagaData.OldId))]
```

**Step 3:** Replace ConfigureMapping
```diff
- protected override string CorrelationPropertyName => nameof(SagaData.OrderId);
- protected override void ConfigureMapping(IMessagePropertyMapper mapper)
- {
-     mapper.ConfigureMapping<StartMessage>(m => m.OrderId);
- }
+ protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
+ {
+     mapper.MapSaga(s => s.OrderId).ToMessage<StartMessage>(m => m.OrderId);
+ }
```

**Step 4:** Update custom finders
```diff
- protected override string CorrelationPropertyName => null;
- protected override void ConfigureMapping(IMessagePropertyMapper mapper)
- {
-     // Empty for custom finders
- }
+ protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
+ {
+     mapper.ConfigureFinderMapping<MyMessage, MyCustomFinder>();
+ }
```

## Impact Assessment

### Positive Impacts:
1. **Unified API:** Aligns SQL Persistence with NServiceBus core saga API
2. **Better Discoverability:** Standard NServiceBus patterns are more familiar to developers
3. **Improved Maintainability:** Less custom code to maintain
4. **Modern C# Features:** Updated to use latest C# language features
5. **Custom Finder Support:** Better integration with custom saga finders

### Challenges:
1. **Breaking Change:** Requires code changes for all users of SqlSaga
2. **Migration Effort:** Teams need to update all saga implementations
3. **Documentation:** Existing documentation needs updating
4. **Compile-Time Errors:** Obsolete attribute with error severity will break builds

## Testing Coverage

The PR includes comprehensive test coverage across:
- ✅ MS SQL Server (Microsoft.Data.SqlClient)
- ✅ MS SQL Server with SQL Transport
- ✅ MySQL
- ✅ PostgreSQL  
- ✅ PostgreSQL with PostgreSQL Transport
- ✅ Oracle
- ✅ Script generation and validation
- ✅ Persistence tests
- ✅ Transactional session tests

All tests have been updated to use the new API, demonstrating the migration path.

## Recommendations

1. **Review Documentation:** Ensure all documentation is updated before merge
2. **Release Notes:** Clearly document this as a breaking change
3. **Migration Guide:** Provide detailed migration instructions
4. **Communication:** Announce this change well in advance
5. **Version Planning:** Consider this for a major version bump (9.0 → 10.0)

## Related PRs and Issues

This PR is part of the NServiceBus 10.0 alpha series which includes:
- Modernization of saga APIs across all persistence packages
- Alignment with NServiceBus core v10 changes
- Deprecation of legacy patterns

## Conclusion

PR #1856 represents a significant modernization of the SQL Persistence saga API, removing the custom `SqlSaga<T>` base class in favor of the standard NServiceBus `Saga<T>` pattern. While this is a breaking change requiring migration effort, it provides long-term benefits through better API consistency, improved maintainability, and alignment with NServiceBus core conventions.

The extensive test coverage (50+ files updated) demonstrates that the migration is straightforward and the new API supports all existing scenarios including custom finders, transitional correlation properties, and table name customization.
