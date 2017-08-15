using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Mono.Cecil;
using NServiceBus;
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
        var saga = (Saga) FormatterServices.GetUninitializedObject(sagaType);
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
        var transitionalCorrelationPropertyName = (string)sagaType
            .GetProperty("TransitionalCorrelationPropertyName", AnyInstanceMember)
            .GetValue(saga);


        CorrelationProperty transitional = null;
        if (transitionalCorrelationPropertyName != null)
        {
            var sagaDataType = sagaType.BaseType.GetGenericArguments()[0];
            var transitionalProperty = sagaDataType.GetProperty(transitionalCorrelationPropertyName, AnyInstanceMember);
            transitional = new CorrelationProperty(transitionalCorrelationPropertyName, CorrelationPropertyTypeReader.GetCorrelationPropertyType(transitionalProperty.PropertyType));
        }

        var tableSuffixOverride = (string)sagaType.GetProperty("TableSuffix", AnyInstanceMember).GetValue(saga);
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
}