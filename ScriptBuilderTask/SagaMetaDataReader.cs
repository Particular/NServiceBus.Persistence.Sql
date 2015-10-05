using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

class SagaMetaDataReader
{
    ModuleDefinition module;
    List<string> skippedTypes = new List<string>();
    Dictionary<string, SagaDataMap> maps = new Dictionary<string, SagaDataMap>();

    public SagaMetaDataReader(ModuleDefinition module)
    {
        this.module = module;
    }

    public IEnumerable<SagaDataMap> GetSagaMaps()
    {
        foreach (var type in module.GetTypes())
        {
            SagaDataMap map;
            if (FindSagaDataType(type, out map))
            {
                yield return map;
            }
        }
    }

    public bool FindSagaDataType(TypeReference type, out SagaDataMap map)
    {
        if (skippedTypes.Contains(type.FullName))
        {
            map = null;
            return false;
        }
        var tryGetValue = maps.TryGetValue(type.FullName, out map);
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
                maps[type.FullName] = map =
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
            maps[type.FullName] = map = new SagaDataMap
            {
                Data = baseMap.Data,
                Saga = type
            };
            return true;
        }
        skippedTypes.Add(type.FullName);
        return false;
    }
}