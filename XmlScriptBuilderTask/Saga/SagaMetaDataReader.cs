using System.Collections.Generic;
using Mono.Cecil;
using NServiceBus.Persistence.Sql.Xml;

class SagaMetaDataReader
{
    ModuleDefinition module;
    BuildLogger buildLogger;

    public SagaMetaDataReader(ModuleDefinition module, BuildLogger buildLogger)
    {
        this.module = module;
        this.buildLogger = buildLogger;
    }

    public IEnumerable<SagaDefinition> GetSagas()
    {
        var sagas = new List<SagaDefinition>();
        foreach (var type in module.GetTypes())
        {
            var baseType = type.BaseType;
            if (baseType == null || baseType.FullName != "NServiceBus.Persistence.Sql.Xml.XmlSagaData")
            {
                continue;
            }
            try
            {
                sagas.Add(BuildSagaDataMap(type));
            }
            catch (ErrorsException exception)
            {
                buildLogger.LogError($"Error in '{type.FullName}'. Error:{exception.Message}", type.GetFileName());
            }
        }
        return sagas;
    }

    public static SagaDefinition BuildSagaDataMap(TypeDefinition type)
    {
        ValidateSagaConventions(type);
        var correlationResult = CorrelationReader.GetCorrelationMember(type);

        return new SagaDefinition
        {
            Name = type.DeclaringType.FullName,
            CorrelationMember = correlationResult.CorrelationMember,
            TransitionalCorrelationMember = correlationResult.TransitionalCorrelationMember
        };
    }

    public static void ValidateSagaConventions(TypeDefinition sagaDataType)
    {
        if (sagaDataType.HasGenericParameters)
        {
            throw new ErrorsException("Saga data types cannot be generic.");
        }
        if (sagaDataType.Name != "SagaData")
        {
            throw new ErrorsException("Saga data types must be named 'SagaData'.");
        }
        if (!sagaDataType.IsNested)
        {
            throw new ErrorsException("Saga data types must be nested under a XmlSaga.");
        }
        if (sagaDataType.HasGenericParameters)
        {
            throw new ErrorsException("Saga data types cannot be generic.");
        }
        var sagaType = sagaDataType.DeclaringType;
        if (sagaType.IsAbstract)
        {
            throw new ErrorsException("Saga types cannot be abstract.");
        }
        if (sagaType.HasGenericParameters)
        {
            throw new ErrorsException("Saga types cannot be generic.");
        }
        if (!sagaType.BaseType.FullName.StartsWith("NServiceBus.Persistence.Sql.Xml.XmlSaga`1"))
        {
            throw new ErrorsException("Saga types must directly inherit from NServiceBus.Persistence.Sql.Xml.XmlSaga<T>.");
        }
    }
}