using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;

class SqlOutboxFeature : Feature
{
    SqlOutboxFeature()
    {
        DependsOn<Outbox>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Settings.EnableFeature<StorageType.Outbox>();
        var settings = context.Settings;
        var connectionString = settings.GetConnectionString<StorageType.Outbox>();
        var schema = settings.GetSchema<StorageType.Outbox>();

        var jsonSerializer = settings.GetJsonSerializer<StorageType.Outbox>();
        var readerCreator = settings.GetReaderCreator<StorageType.Outbox>();
        var writerCreator = settings.GetWriterCreator<StorageType.Outbox>();

        var endpointName = settings.GetEndpointNamePrefix<StorageType.Outbox>();
        var outboxPersister = new OutboxPersister(connectionString, schema, endpointName, jsonSerializer, readerCreator, writerCreator);
        context.Container.ConfigureComponent(b => outboxPersister, DependencyLifecycle.InstancePerCall);
    }
}