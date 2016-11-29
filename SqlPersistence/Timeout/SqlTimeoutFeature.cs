using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Timeout.Core;

class SqlTimeoutFeature : Feature
{

    SqlTimeoutFeature()
    {
        DependsOn<TimeoutManager>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        settings.EnableFeature<StorageType.Timeouts>();
        var connectionString = settings.GetConnectionString<StorageType.Timeouts>();
        var schema = settings.GetSchema<StorageType.Timeouts>();
        var serializer = settings.GetJsonSerializer<StorageType.Timeouts>();
        var readerCreator = settings.GetReaderCreator<StorageType.Timeouts>();
        var writerCreator = settings.GetWriterCreator<StorageType.Timeouts>();

        var endpointName = settings.GetEndpointNamePrefix<StorageType.Timeouts>();
        var persister = new TimeoutPersister(connectionString, schema, endpointName, serializer, readerCreator, writerCreator);
        context.Container.RegisterSingleton(typeof(IPersistTimeouts), persister);
        context.Container.RegisterSingleton(typeof(IQueryTimeouts), persister);
    }
}