using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using NServiceBus;
using NServiceBus.Persistence.Sql;
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

        var sagaAssembly = sagaMetadataCollection.First().SagaType.Assembly;
        //Validate the saga definitions using script builder compile-time validation
        using (var moduleDefinition = ModuleDefinition.ReadModule(sagaAssembly.Location, new ReaderParameters(ReadingMode.Deferred)))
        {
            var compileTimeReader = new AllSagaDefinitionReader(moduleDefinition);
            compileTimeReader.GetSagas();
        }

        return sagaMetadataCollection.Select(metadata => GetSagaDefinition(metadata.SagaType, sqlDialect));
    }

    public static SagaDefinition GetSagaDefinition(Type sagaType, BuildSqlDialect sqlDialect)
    {
        if (SagaTypeHasIntermediateBaseClass(sagaType))
        {
            throw new Exception("Saga implementations must inherit from either Saga<T> or SqlSaga<T> directly. Deep class hierarchies are not supported.");
        }

        var saga = (Saga)RuntimeHelpers.GetUninitializedObject(sagaType);
        var mapper = new ConfigureHowToFindSagaWithMessage();
        methodInfo.Invoke(saga, [mapper]);
        CorrelationProperty correlationProperty = null;
        if (mapper.CorrelationType != null)
        {
            correlationProperty = new CorrelationProperty(
                name: mapper.CorrelationProperty,
                type: CorrelationPropertyTypeReader.GetCorrelationPropertyType(mapper.CorrelationType));
        }

        var transitionalCorrelationPropertyName = GetSagaMetadataProperty(sagaType, att => att.TransitionalCorrelationProperty);

        CorrelationProperty transitional = null;
        if (transitionalCorrelationPropertyName != null)
        {
            var sagaDataType = sagaType.BaseType.GetGenericArguments()[0];
            var transitionalProperty = sagaDataType.GetProperty(transitionalCorrelationPropertyName, AnyInstanceMember);
            transitional = new CorrelationProperty(transitionalCorrelationPropertyName, CorrelationPropertyTypeReader.GetCorrelationPropertyType(transitionalProperty.PropertyType));
        }

        var tableSuffixOverride = GetSagaMetadataProperty(sagaType, att => att.TableSuffix);
        var tableSuffix = tableSuffixOverride ?? sagaType.Name;

        if (sqlDialect == BuildSqlDialect.Oracle)
        {
            tableSuffix = tableSuffix.Substring(0, Math.Min(27, tableSuffix.Length));
        }

        return new SagaDefinition(
            tableSuffix: tableSuffix,
            name: sagaType.FullName,
            correlationProperty: correlationProperty,
            transitionalCorrelationProperty: transitional);
    }

    static bool SagaTypeHasIntermediateBaseClass(Type sagaType)
    {
        var baseType = sagaType.BaseType;
        if (!baseType.IsGenericType)
        {
            // Saga<T> and SqlSaga<T> are both generic types
            return true;
        }

        var genericBase = baseType.GetGenericTypeDefinition();
        return genericBase != typeof(Saga<>);
    }

    static string GetSagaMetadataProperty(Type sagaType, Func<SqlSagaAttribute, string> getSqlSagaAttributeValue)
    {
        if (sagaType.IsSubclassOfRawGeneric(typeof(Saga<>)))
        {
            var attr = sagaType.GetCustomAttribute<SqlSagaAttribute>();
            return attr != null ? getSqlSagaAttributeValue(attr) : null;
        }

        throw new Exception($"Type '{sagaType.FullName}' is not a Saga<T>.");
    }

    static readonly MethodInfo methodInfo = typeof(Saga).GetMethod("ConfigureHowToFindSaga", BindingFlags.NonPublic | BindingFlags.Instance);
    const BindingFlags AnyInstanceMember = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
}