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

        return sagaMetadataCollection.Select(metadata => GetSagaDefinition(metadata.SagaType, sagaDefinitions, sqlDialect));
    }

    public static SagaDefinition GetSagaDefinition<TSagaType>(BuildSqlDialect sqlDialect)
        where TSagaType : Saga
    {
        var sagaDefinitions = GetSagaDefinitions([typeof(TSagaType).Assembly]);
        var metadata = SagaMetadata.Create<TSagaType>();

        return GetSagaDefinition(metadata.SagaType, sagaDefinitions, sqlDialect);
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

    static SagaDefinition GetSagaDefinition(Type sagaType, Dictionary<string, SagaDefinition> definitions, BuildSqlDialect sqlDialect)
    {
        if (!definitions.TryGetValue(sagaType.FullName!.Replace('+', '.'), out var sagaDefinition))
        {
            throw new Exception($"Could not find metadata for '{sagaType.FullName}' in the collected assembly metadata.");
        }

        var tableSuffix = sagaDefinition.TableSuffix;
        if (sqlDialect == BuildSqlDialect.Oracle)
        {
            tableSuffix = sagaDefinition.TableSuffix[..Math.Min(27, sagaDefinition.TableSuffix.Length)];
        }

        return new SagaDefinition(
            tableSuffix: tableSuffix,
            name: sagaType.FullName,
            correlationProperty: sagaDefinition.CorrelationProperty,
            transitionalCorrelationProperty: sagaDefinition.TransitionalCorrelationProperty);
    }
}