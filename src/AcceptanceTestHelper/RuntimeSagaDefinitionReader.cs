using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using NServiceBus;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public static class RuntimeSagaDefinitionReader
{
    static MethodInfo methodInfo = typeof(Saga).GetMethod("ConfigureHowToFindSaga", BindingFlags.NonPublic | BindingFlags.Instance);

    public static IEnumerable<SagaDefinition> GetSagaDefinitions(EndpointConfiguration endpointConfiguration)
    {
        return endpointConfiguration.GetScannedSagaTypes().Select(GetSagaDefinition);
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