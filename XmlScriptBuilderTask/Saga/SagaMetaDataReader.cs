using System.Collections.Generic;
using System.Linq;
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
        foreach (var dataType in module.GetTypes())
        {
            if (IsSagaDataType(dataType))
            {
                try
                {
                    sagas.Add(BuildSagaDataMap(dataType));
                }
                catch (ErrorsException exception)
                {
                    buildLogger?.LogError($"Error in '{dataType.FullName}'. Error:{exception.Message}", dataType.GetFileName());
                }
            }
        }
        return sagas;
    }

    bool IsSagaDataType(TypeDefinition type)
    {
        if (type.HasGenericParameters || type.IsAbstract)
        {
            return false;
        }
        if (type.BaseType != null && type.BaseType.FullName == "NServiceBus.ContainSagaData")
        {
            return true;
        }
        if (type.Interfaces.Any(i => i.FullName == "NServiceBus.IContainSagaData"))
        {
            return true;
        }
        return false;
    }

    //TypeReference GetNestedSagaDataTypeReference(TypeDefinition type)
    //{
    //    if (type.HasGenericParameters)
    //    {
    //        return null;
    //    }
    //    var elementType = type.BaseType?.GetElementType();
    //    if (elementType == null)
    //    {
    //        return null;
    //    }
    //    if (elementType.FullName != "NServiceBus.Saga`1" && elementType.FullName != "NServiceBus.Persistence.SqlServerXml.XmlSaga`1")
    //    {
    //        return null;
    //    }
    //    var genericInstanceType = (GenericInstanceType) type.BaseType;
    //    return genericInstanceType.GenericArguments[0];
    //}

    public static SagaDefinition BuildSagaDataMap(TypeDefinition sagaDataType)
    {
        ValidateSagaConventions(sagaDataType);
        var correlationResult = CorrelationReader.GetCorrelationMember(sagaDataType);

        return new SagaDefinition
        {
            Name = BuildSagaName(sagaDataType),
            CorrelationMember = correlationResult.CorrelationMember,
            TransitionalCorrelationMember = correlationResult.TransitionalCorrelationMember
        };
    }

    static string BuildSagaName(TypeDefinition sagaDataType)
    {
        if (sagaDataType.IsNested)
        {
            var parent = sagaDataType.DeclaringType;
            var fullName = parent.BaseType.FullName;
            if (parent.BaseType != null && (fullName.StartsWith("NServiceBus.Saga`") || fullName.StartsWith("NServiceBus.Persistence.Sql.Xml.XmlSaga`1")))
            {
                return parent.Name;
            }
        }
        return sagaDataType.Name;
    }

    public static void ValidateSagaConventions(TypeDefinition sagaDataType)
    {
        if (sagaDataType.HasGenericParameters)
        {
            throw new ErrorsException("Saga data types cannot be generic.");
        }
        if (!sagaDataType.IsNested)
        {
            throw new ErrorsException("Saga data types must be nested under a Saga.");
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
    }
}