using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using NServiceBus.Saga;
using NServiceBus.SqlPersistence;

static class SagaMetaDataReader 
{
    static Type baseSagaType = typeof(Saga);
    static Type genericBaseSagaType = typeof(Saga<>);

    internal static IEnumerable<SagaDefinition> GetSagaDefinitions(IEnumerable<Type> typesToScan)
    {
        foreach (var sagaType in GetSagaTypes(typesToScan))
        {
            Type sagaDataType;
            if (!TryGetSagaDataType(sagaType, out sagaDataType))
            {
                continue;
            }
            yield return new SagaDefinition
            {
                Name = SagaTableNameBuilder.GetTableSuffix(sagaDataType),
                MappedProperties = GetMappedProperties(sagaType),
                UniqueProperties = GetUniquePropertyNames(sagaDataType)
            };
        }
    }

    internal static IEnumerable<Type> GetSagaTypes(IEnumerable<Type> typesToScan)
    {
        return typesToScan
            .Where(IsSagaClass);
    }

    internal static List<string> GetMappedProperties(Type sagaDataType)
    {
        var instance = FormatterServices.GetUninitializedObject(sagaDataType);
        var configureHowToFindSaga = sagaDataType.GetMethod("ConfigureHowToFindSaga", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(IConfigureHowToFindSagaWithMessage)}, null);
        var mapper = new SagaPropertyMapper();
        configureHowToFindSaga.Invoke(instance, new object[] {mapper});
        return mapper.Properties;
    }

    internal static bool TryGetSagaDataType(Type sagaType, out Type sagaDataType)
    {
        while (true)
        {
            if (sagaType == typeof (object))
            {
                sagaDataType = null;
                return false;
            }
            if (sagaType.BaseType.IsGenericType && (sagaType.BaseType.GetGenericTypeDefinition() == genericBaseSagaType))
            {
                sagaDataType = sagaType.BaseType.GetGenericArguments().First();
                return true;
            }

            sagaType = sagaType.BaseType;
        }

    }

    static List<string> GetUniquePropertyNames(Type sagaDataType)
    {
        return UniqueAttribute.GetUniqueProperties(sagaDataType)
            .Select(x => x.Name)
            .ToList();
    }

    static bool IsSagaClass(Type type)
    {
        return baseSagaType.IsAssignableFrom(type)
            && !type.IsAbstract;
    }
}