using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;

class SqlXmlOutboxFeature : Feature
{
    SqlXmlOutboxFeature()
    {
        DependsOn<Outbox>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Settings.EnableFeature<StorageType.Outbox>();
        var settings = context.Settings;
        var connectionString = settings.GetConnectionString<StorageType.Outbox>();
        var schema = settings.GetSchema<StorageType.Outbox>();
        var endpointName = settings.ShouldUseEndpointName<StorageType.Outbox>()
            ? settings.EndpointName() + "."
            : "";
        var outboxPersister = new OutboxPersister(connectionString, schema, endpointName);
        context.Container.ConfigureComponent(b => outboxPersister, DependencyLifecycle.InstancePerCall);
    }
}