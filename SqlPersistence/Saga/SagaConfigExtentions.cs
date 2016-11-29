using System;
using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Settings;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;

public static class SagaConfigExtentions
{
 
    public static void SerializeBuilder<TSerializer, TReader>(this PersistenceExtensions<SqlXmlPersistence, StorageType.Sagas> configuration, SagaSerializeBuilder<TReader> builder) 
        where TSerializer : SqlPersistenceSerializer<TReader>, new() 
        where TReader : IDisposable
    {
        configuration.GetSettings()
            .Set<SagaSerializeBuilder<TReader>>(builder);
    }

    internal static SagaSerializeBuilder<TReader> GetSerializeBuilder<TReader>(this ReadOnlySettings settings)
    {
        SagaSerializeBuilder<TReader> value;
        settings.TryGet(out value);
        return value;
    }

    public static void VersionDeserializeBuilder<TSerializer, TReader>(this PersistenceExtensions<SqlXmlPersistence, StorageType.Sagas> configuration, VersionDeserializeBuilder<TReader> builder) 
        where TSerializer : SqlPersistenceSerializer<TReader>, new() 
        where TReader : IDisposable
    {
        configuration.GetSettings()
            .Set<VersionDeserializeBuilder<TReader>>(builder);
    }

    internal static VersionDeserializeBuilder<TReader> GetVersionDeserializeBuilder<TReader>(this ReadOnlySettings settings)
    {
        VersionDeserializeBuilder<TReader> value;
        settings.TryGet(out value);
        return value;
    }

}