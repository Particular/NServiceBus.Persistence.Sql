using System.Xml;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;

class SqlXmlSagaFeature : Feature
{
    SqlXmlSagaFeature()
    {
        DependsOn<Sagas>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Settings.EnableFeature<StorageType.Sagas>();

        var settings = context.Settings;
        var schema = settings.GetSchema<StorageType.Sagas>();

        var endpointName = settings.GetEndpointNamePrefix<StorageType.Sagas>();
        var commandBuilder = new SagaCommandBuilder(schema, endpointName);

        var serialize = settings.GetSerializeBuilder<XmlReader>();
        var versionDeserializeBuilder = settings.GetVersionDeserializeBuilder<XmlReader>();

        var sqlPersistenceSerializer = new XmlPersistenceSerializer();
        sqlPersistenceSerializer.SetSerializeBuilder(serialize);
        sqlPersistenceSerializer.SetVersionDeserializeBuilder(versionDeserializeBuilder);

        var infoCache = new SagaInfoCache<XmlReader>(commandBuilder, sqlPersistenceSerializer);
        var sagaPersister = new SagaPersister<XmlReader>(infoCache, sqlPersistenceSerializer);
        context.Container.ConfigureComponent<ISagaPersister>(() => sagaPersister, DependencyLifecycle.SingleInstance);
    }
}