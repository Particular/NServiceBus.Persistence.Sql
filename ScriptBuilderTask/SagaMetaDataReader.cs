using System.Collections.Generic;
using Mono.Cecil;
using NServiceBus.SqlPersistence;

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
        foreach (var type in module.GetTypes())
        {
            if (type.BaseType != null && type.BaseType.FullName == "NServiceBus.SqlPersistence.XmlSagaData")
            {
                string error;
                SagaDefinition saga;
                if (TryBuildSagaDataMap(type, out saga, out error))
                {
                    yield return saga;
                    continue;
                }
                var error1 = new FileError
                {
                    File = type.GetFileName(),
                    Message = $"Error in '{type.FullName}'. Error:{error}"
                };
                buildLogger.LogError(error1);
            }
        }
    }

    public static bool TryBuildSagaDataMap(TypeDefinition type, out SagaDefinition sagaDefinition, out string error)
    {
        if (!ValidateSagaConventions(type, out error))
        {
            sagaDefinition = null;
            return false;
        }
        var correlationResult = CorrelationReader.GetCorrelationMember(type);
        if (correlationResult.Errored)
        {
            error = correlationResult.Error;
            sagaDefinition = null;
            return false;
        }

        sagaDefinition = new SagaDefinition
        {
            Name = type.DeclaringType.FullName,
            CorrelationMember = correlationResult.Name
        };
        error = null;
        return true;
    }

    public static bool ValidateSagaConventions(TypeDefinition sagaDataType, out string error)
    {
        if (sagaDataType.HasGenericParameters)
        {
            error = "Saga data types cannot be generic.";
            return false;
        }
        if (sagaDataType.Name != "SagaData")
        {
            error = "Saga data types must be named 'SagaData'.";
            return false;
        }
        if (!sagaDataType.IsNested)
        {
            error = "Saga data types must be nested under a XmlSaga.";
            return false;
        }
        if (sagaDataType.HasGenericParameters)
        {
            error = "Saga data types cannot be generic.";
            return false;
        }
        var sagaType = sagaDataType.DeclaringType;
        if (sagaType.IsAbstract)
        {
            error = "Saga types cannot be abstract.";
            return false;
        }
        if (sagaType.HasGenericParameters)
        {
            error = "Saga types cannot be generic.";
            return false;
        }
        if (!sagaType.BaseType.FullName.StartsWith("NServiceBus.SqlPersistence.XmlSaga`1"))
        {
            error = "Saga types must directly inherit from NServiceBus.SqlPersistence.XmlSaga<T>.";
            return false;
        }
        foreach (var property in sagaDataType.Properties)
        {
            if (property.ContainsAttribute("NServiceBus.Saga.UniqueAttribute"))
            {
                error = "UniqueAttribute are not supported. Property: " + property.Name;
                return false;
            }
        }
        error = null;
        return true;
    }
}