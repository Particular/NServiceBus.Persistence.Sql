using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NServiceBus;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Sagas;
using NServiceBus.Settings;

public static class RuntimeSagaDefinitionReader
{
    public static IEnumerable<SagaDefinition> GetSagaDefinitions(IReadOnlySettings settings, BuildSqlDialect sqlDialect)
    {
        var sagaMetadataCollection = settings.GetOrDefault<SagaMetadataCollection>() ?? [];

        if (!sagaMetadataCollection.Any())
        {
            return [];
        }

        var sagaDefinitions = GetSagaDefinitions(sagaMetadataCollection.Select(m => m.SagaType.Assembly).Distinct());

        // The runtime applies this filter to every saga table suffix, so the tables created here must use it too.
        var tableSuffixFilter = NServiceBus.Persistence.Sql.SagaSettings.GetNameFilter(settings) ?? (static suffix => suffix);

        return sagaMetadataCollection.Select(metadata => GetSagaDefinition(metadata.SagaType, sagaDefinitions, tableSuffixFilter));
    }

    public static SagaDefinition GetSagaDefinition<TSagaType>(BuildSqlDialect sqlDialect)
        where TSagaType : Saga
    {
        var sagaDefinitions = GetSagaDefinitions([typeof(TSagaType).Assembly]);
        var metadata = SagaMetadata.Create<TSagaType>();

        Func<string, string> tableSuffixFilter = sqlDialect == BuildSqlDialect.Oracle
            ? suffix => OracleSagaTableNames.Create(OracleSagaTableNames.AcceptanceTestsPrefix, suffix)
            : static suffix => suffix;

        return GetSagaDefinition(metadata.SagaType, sagaDefinitions, tableSuffixFilter);
    }

    static Dictionary<string, SagaDefinition> GetSagaDefinitions(IEnumerable<Assembly> sagaAssemblies)
    {
        var sagaDefinitions = new List<SagaDefinition>();
        foreach (var assembly in sagaAssemblies)
        {
            //Validate the saga definitions using script builder compile-time validation
            var settings = SettingsAttributeReader.Read(assembly.Location);
            sagaDefinitions.AddRange(settings.SagaDefinitions);
        }

        var definitions = sagaDefinitions.ToDictionary(static s => s.Name.Replace("/", "+"), static s => s);
        return definitions;
    }

    static SagaDefinition GetSagaDefinition(Type sagaType, Dictionary<string, SagaDefinition> definitions, Func<string, string> tableSuffixFilter)
    {
        if (!definitions.TryGetValue(sagaType.FullName!.Replace('+', '.'), out var sagaDefinition))
        {
            throw new Exception($"Could not find metadata for '{sagaType.FullName}' in the collected assembly metadata.");
        }

        return new SagaDefinition(
            tableSuffix: tableSuffixFilter(sagaDefinition.TableSuffix),
            name: sagaType.FullName,
            correlationProperty: sagaDefinition.CorrelationProperty,
            transitionalCorrelationProperty: sagaDefinition.TransitionalCorrelationProperty);
    }
}