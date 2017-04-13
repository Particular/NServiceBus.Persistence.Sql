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

    public static IEnumerable<SagaDefinition> GetSagaDefinitions(EndpointConfiguration endpointConfiguration)
    {
        var sagaTypes = endpointConfiguration.GetScannedSagaTypes().ToArray();
        if (!sagaTypes.Any())
        {
            return Enumerable.Empty<SagaDefinition>();
        }
        var sagaAssembly = sagaTypes.First().Assembly;
        //Validate the saga definitions using script builder compile-time validation
        var moduleDefinition = ModuleDefinition.ReadModule(sagaAssembly.Location, new ReaderParameters(ReadingMode.Deferred));
        var compileTimeReader = new AllSagaDefinitionReader(moduleDefinition);
        var exceptions = new List<Exception>();
        compileTimeReader.GetSagas((e, d) =>
        {
            exceptions.Add(e);
        });
        if (exceptions.Any())
        {
            throw new AggregateException(exceptions);
        }
        return sagaTypes.Select(GetSagaDefinition);
    }

    static SagaDefinition GetSagaDefinition(Type sagaType)
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
        return new SagaDefinition(
            tableSuffix: sagaType.Name,
            name: sagaType.FullName,
            correlationProperty: correlationProperty);
    }
}