using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

class SagaMetaDataReader
{
    ModuleDefinition module;
    List<string> skippedTypes = new List<string>();
    Dictionary<string, SagaDataMap> sagaToSagaDataMapping = new Dictionary<string, SagaDataMap>();

    public SagaMetaDataReader(ModuleDefinition module)
    {
        this.module = module;
    }

    public void Foo()
    {
        foreach (var type in module.GetTypes())
        {
            SagaDataMap fake;
            if (FindSagaDataType(type, out fake))
            {
            }
        }
        
    }

    public bool FindSagaDataType(TypeReference type, out SagaDataMap sagaDataMap)
    {
        if (skippedTypes.Contains(type.FullName))
        {
            sagaDataMap = null;
            return false;
        }
        var tryGetValue = sagaToSagaDataMapping.TryGetValue(type.FullName, out sagaDataMap);
        if (tryGetValue)
        {
            return true;
        }

        if (!type.ReferencesNServiceBus())
        {
            skippedTypes.Add(type.FullName);
            return false;
        }

        var baseType = type.GetBase();
        var genericInstanceType = baseType as GenericInstanceType;
        if (genericInstanceType != null)
        {
            if (baseType.IsSaga())
            {
                var sagaDataType = genericInstanceType.GenericArguments.First().Resolve();
                sagaToSagaDataMapping[type.FullName] = sagaDataMap =
                    new SagaDataMap
                    {
                        Data = sagaDataType,
                        Saga = type
                    };
                return true;
            }
        }
        SagaDataMap baseMap;
        if (FindSagaDataType(baseType, out baseMap))
        {
            sagaToSagaDataMapping[type.FullName] = sagaDataMap = new SagaDataMap
            {
                Data = baseMap.Data,
                Saga = type
            };
            return true;
        }
        return false;
    }
}

class SagaDataMap
{
    public TypeReference Saga;
    public TypeDefinition Data;
}