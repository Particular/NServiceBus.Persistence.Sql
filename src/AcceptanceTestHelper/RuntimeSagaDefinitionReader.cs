using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Sagas;
using NServiceBus.Settings;

public static class RuntimeSagaDefinitionReader
{
    public static IEnumerable<SagaDefinition> GetSagaDefinitions(IReadOnlySettings settings, BuildSqlDialect sqlDialect)
    {
        var sagaMetadataCollection = settings.GetOrDefault<SagaMetadataCollection>() ?? new SagaMetadataCollection();

        if (!sagaMetadataCollection.Any())
        {
            return [];
        }

        var sagaDefinitions = GetSagaDefinitions(sagaMetadataCollection.Select(m => m.SagaType.Assembly).Distinct());

        return sagaMetadataCollection.Select(metadata => GetSagaDefinition(metadata.SagaType, sagaDefinitions, sqlDialect));
    }

    public static SagaDefinition GetSagaDefinition(Type sagaType, BuildSqlDialect sqlDialect)
    {
        var sagaDefinitions = GetSagaDefinitions([sagaType.Assembly]);
        var metadata = SagaMetadata.Create(sagaType);

        return GetSagaDefinition(metadata.SagaType, sagaDefinitions, sqlDialect);
    }

    static Dictionary<string, SagaDefinition> GetSagaDefinitions(IEnumerable<Assembly> sagaAssemblies)
    {
        var sagaDefinitions = new List<SagaDefinition>();
        foreach (var assembly in sagaAssemblies)
        {
            //Validate the saga definitions using script builder compile-time validation
            using var moduleDefinition = ModuleDefinition.ReadModule(assembly.Location, new ReaderParameters(ReadingMode.Deferred));
            var compileTimeReader = new AllSagaDefinitionReader(moduleDefinition);
            sagaDefinitions.AddRange(compileTimeReader.GetSagas());
        }

        var definitions = sagaDefinitions.ToDictionary(static s => s.Name.Replace("/", "+"), static s => s);
        return definitions;
    }

    static SagaDefinition GetSagaDefinition(Type sagaType, Dictionary<string, SagaDefinition> definitions, BuildSqlDialect sqlDialect)
    {
        if (!definitions.TryGetValue(sagaType.FullName!, out var sagaDefinition))
        {
            throw new Exception($"Type '{sagaType.FullName}' is not a Saga<T>.");
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