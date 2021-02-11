using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Mono.Cecil;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public static class RuntimeSagaDefinitionReader
{
    static MethodInfo methodInfo = typeof(Saga).GetMethod("ConfigureHowToFindSaga", BindingFlags.NonPublic | BindingFlags.Instance);
    const BindingFlags AnyInstanceMember = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static IEnumerable<SagaDefinition> GetSagaDefinitions(EndpointConfiguration endpointConfiguration, BuildSqlDialect sqlDialect)
    {
        var sagaTypes = endpointConfiguration.GetScannedSagaTypes().ToArray();
        if (!sagaTypes.Any())
        {
            return Enumerable.Empty<SagaDefinition>();
        }
        var sagaAssembly = sagaTypes.First().Assembly;
        var exceptions = new List<Exception>();
        //Validate the saga definitions using script builder compile-time validation
        using (var moduleDefinition = ModuleDefinition.ReadModule(sagaAssembly.Location, new ReaderParameters(ReadingMode.Deferred)))
        {
            var compileTimeReader = new AllSagaDefinitionReader(moduleDefinition);

            compileTimeReader.GetSagas((e, d) =>
            {
                exceptions.Add(e);
            });
        }
        if (exceptions.Any())
        {
            throw new AggregateException(exceptions);
        }
        return sagaTypes.Select(sagaType => GetSagaDefinition(sagaType, sqlDialect));
    }

    public static SagaDefinition GetSagaDefinition(Type sagaType, BuildSqlDialect sqlDialect)
    {
        if (SagaTypeHasIntermediateBaseClass(sagaType))
        {
            throw new Exception("Saga implementations must inherit from either Saga<T> or SqlSaga<T> directly. Deep class hierarchies are not supported.");
        }

        var saga = (Saga)FormatterServices.GetUninitializedObject(sagaType);
        var mapper = new ConfigureHowToFindSagaWithMessage();
        methodInfo.Invoke(saga, new object[]
        {
            mapper
        });
        CorrelationProperty correlationProperty = null;
        if (mapper.CorrelationType != null)
        {
            correlationProperty = new CorrelationProperty(
                name: mapper.CorrelationProperty,
                type: CorrelationPropertyTypeReader.GetCorrelationPropertyType(mapper.CorrelationType));
        }

        var transitionalCorrelationPropertyName = GetSagaMetadataProperty(sagaType, saga, "TransitionalCorrelationPropertyName", att => att.TransitionalCorrelationProperty);

        CorrelationProperty transitional = null;
        if (transitionalCorrelationPropertyName != null)
        {
            var sagaDataType = sagaType.BaseType.GetGenericArguments()[0];
            var transitionalProperty = sagaDataType.GetProperty(transitionalCorrelationPropertyName, AnyInstanceMember);
            transitional = new CorrelationProperty(transitionalCorrelationPropertyName, CorrelationPropertyTypeReader.GetCorrelationPropertyType(transitionalProperty.PropertyType));
        }

        var tableSuffixOverride = GetSagaMetadataProperty(sagaType, saga, "TableSuffix", att => att.TableSuffix);
        var tableSuffix = tableSuffixOverride ?? sagaType.Name;

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
        return genericBase != typeof(Saga<>) && genericBase != typeof(SqlSaga<>);
    }

    static string GetSagaMetadataProperty(Type sagaType, Saga instance, string sqlSagaPropertyName, Func<SqlSagaAttribute, string> getSqlSagaAttributeValue)
    {
        if (sagaType.IsSubclassOfRawGeneric(typeof(SqlSaga<>)))
        {
            return (string)sagaType
                .GetProperty(sqlSagaPropertyName, AnyInstanceMember)
                .GetValue(instance);
        }

        if (sagaType.IsSubclassOfRawGeneric(typeof(Saga<>)))
        {
            var attr = sagaType.GetCustomAttribute<SqlSagaAttribute>();
            return (attr != null) ? getSqlSagaAttributeValue(attr) : null;
        }

        throw new Exception($"Type '{sagaType.FullName}' is not a Saga<T>.");
    }
}