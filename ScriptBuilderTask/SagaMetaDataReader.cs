using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;


class SagaMetaDataReader
{
    ModuleDefinition module;
    List<string> skippedTypes = new List<string>();
    Dictionary<string, TypeReference> sagaToSagaDataMapping = new Dictionary<string, TypeReference>();

    public SagaMetaDataReader(ModuleDefinition module)
    {
        this.module = module;
    }

    public void Foo()
    {
        foreach (var type in module.GetTypes())
        {
            TypeReference fake;
            FindSagaDataType(type, out fake);
        }
    }

    public bool FindSagaDataType(TypeDefinition type, out TypeReference sagaDataType)
    {
        if (skippedTypes.Contains(type.FullName))
        {
            sagaDataType = null;
            return false;
        }
        if (sagaToSagaDataMapping.TryGetValue(type.FullName, out sagaDataType))
        {
            return true;
        }

        if (!type.Module.AssemblyReferences.Any(x => x.Name.StartsWith("NServiceBus.Core")))
        {
            skippedTypes.Add(type.FullName);
            return false;
        }


        var baseType = type.BaseType;
        var genericInstanceType = baseType as GenericInstanceType;
        if (genericInstanceType != null)
        {
            if (baseType.FullName.StartsWith("NServiceBus.Saga.Saga`1"))
            {
                sagaDataType = genericInstanceType.GenericArguments.First();
                sagaToSagaDataMapping[type.FullName] = sagaDataType;
                return true;
            }
        }
        return FindSagaDataType(baseType.Resolve(), out sagaDataType);
    }
}

class SagaDataInfo
{
    public List<string> MappedProperties = new List<string>();
    public List<string> UniqueProperties = new List<string>();
}