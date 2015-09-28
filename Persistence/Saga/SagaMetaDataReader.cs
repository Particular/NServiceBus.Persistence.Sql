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
            if (!TryGetBaseSagaType(sagaType, out sagaDataType))
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

    internal static List<string> GetMappedProperties(Type sagaType)
    {
        var instance = FormatterServices.GetUninitializedObject(sagaType);
        var configureHowToFindSaga = sagaType.GetMethod("ConfigureHowToFindSaga", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(IConfigureHowToFindSagaWithMessage)}, null);
        var mapper = new SagaPropertyMapper();
        configureHowToFindSaga.Invoke(instance, new object[] {mapper});
        return mapper.Properties;
    }

    internal static bool TryGetBaseSagaType(Type type, out Type sagaDataType)
    {
        while (true)
        {
            if (type == typeof (object))
            {
                sagaDataType = null;
                return false;
            }
            if (type.BaseType.IsGenericType && (type.BaseType.GetGenericTypeDefinition() == genericBaseSagaType))
            {
                sagaDataType = type.BaseType.GetGenericArguments().First();
                return true;
            }

            type = type.BaseType;
        }

    }

    static List<string> GetUniquePropertyNames(Type sagaType)
    {
        return UniqueAttribute.GetUniqueProperties(sagaType)
            .Select(x => x.Name)
            .ToList();
    }

    static bool IsSagaClass(Type type)
    {
        return baseSagaType.IsAssignableFrom(type)
            && !type.IsAbstract;
    }
}