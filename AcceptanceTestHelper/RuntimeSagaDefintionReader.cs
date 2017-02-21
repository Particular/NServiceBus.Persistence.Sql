using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using NServiceBus;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public static class RuntimeSagaDefintionReader
{
    static MethodInfo methodInfo = typeof(Saga).GetMethod("ConfigureHowToFindSaga", BindingFlags.NonPublic | BindingFlags.Instance);

    public static IEnumerable<SagaDefinition> GetSagaDefintions(EndpointConfiguration endpointConfiguration)
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
        return new SagaDefinition(
            tableSuffix: sagaType.Name,
            name: sagaType.FullName,
            correlationProperty: new CorrelationProperty(
                name: mapper.CorrelationProperty,
                type: CorrelationPropertyTypeReader.GetCorrelationPropertyType(mapper.CorrelationType)));
    }
}