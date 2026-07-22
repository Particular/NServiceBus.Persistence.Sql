using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;
using NServiceBus.Settings;
using NUnit.Framework;
using SagaSettings = NServiceBus.Persistence.Sql.SagaSettings;

[TestFixture]
public class SqlSagaFeatureTests
{
    [Test]
    public void Saga_manifest_uses_sql_saga_table_suffix_for_oracle()
    {
        var manifest = SqlSagaFeature.BuildSagaManifests(
            MetadataFor(typeof(SagaWithEntityNameLongerThanOracleAllows)),
            new SqlDialect.Oracle(),
            "Endpoint_",
            sagaName => sagaName).Single();

        Assert.That(manifest.TableName, Is.EqualTo("\"SHORTSAGATABLE\""));
        Assert.That(manifest.Indexes.Single().Name, Is.EqualTo("Index_Correlation_CorrelationProperty"));
        Assert.That(manifest.Indexes.Single().Columns, Is.EqualTo("CorrelationProperty"));
    }

    [Test]
    public void Saga_manifest_uses_sql_saga_table_suffix()
    {
        var manifest = SqlSagaFeature.BuildSagaManifests(
            MetadataFor(typeof(SagaWithEntityNameLongerThanOracleAllows)),
            new SqlDialect.MsSqlServer(),
            "Endpoint_",
            sagaName => sagaName).Single();

        Assert.That(manifest.TableName, Is.EqualTo("[dbo].[Endpoint_ShortSagaTable]"));
    }

    [Test]
    public void Saga_manifest_uses_saga_name_not_saga_data_name()
    {
        var manifest = SqlSagaFeature.BuildSagaManifests(
            MetadataFor(typeof(PlainSaga)),
            new SqlDialect.MsSqlServer(),
            "Endpoint_",
            sagaName => sagaName).Single();

        Assert.That(manifest.TableName, Is.EqualTo("[dbo].[Endpoint_PlainSaga]"));
    }

    [Test]
    public void Saga_manifest_table_name_matches_runtime_table_name()
    {
        var metadata = MetadataFor(typeof(SagaWithEntityNameLongerThanOracleAllows));
        var dialect = new SqlDialect.MsSqlServer();

        var manifest = SqlSagaFeature.BuildSagaManifests(metadata, dialect, "Endpoint_", sagaName => sagaName).Single();

        var runtime = dialect.GetSagaTableName(
            "Endpoint_", SqlSagaTypeDataReader.GetTypeData(metadata.Single()).TableSuffix);

        Assert.That(manifest.TableName, Is.EqualTo(runtime));
    }

    [Test]
    public void Saga_manifest_index_honours_attribute_correlation_property_override()
    {
        var manifest = SqlSagaFeature.BuildSagaManifests(
            MetadataFor(typeof(SagaWithOverriddenCorrelationProperty)),
            new SqlDialect.MsSqlServer(),
            "Endpoint_",
            sagaName => sagaName).Single();

        // The [SqlSaga] attribute overrides the correlation property, so the manifest index must
        // follow the attribute (as the runtime does) rather than the property inferred from the mapper.
        Assert.That(manifest.Indexes.Single().Name, Is.EqualTo("Index_Correlation_AttributeChosenProperty"));
        Assert.That(manifest.Indexes.Single().Columns, Is.EqualTo("AttributeChosenProperty"));
    }

    [Test]
    public void Saga_manifest_applies_name_filter()
    {
        var settings = SettingsFor("Endpoint", typeof(PlainSaga));
        new SagaSettings(settings).NameFilter(_ => "Filtered");
        var manifest = ManifestFor(settings);

        SqlSagaFeature.ConfigureSagaManifest(manifest, settings, new SqlDialect.MsSqlServer());

        Assert.That(manifest.Sagas.Single().TableName, Is.EqualTo("[dbo].[Endpoint_Filtered]"));
    }

    [Test]
    public void Saga_manifest_prefix_cleans_endpoint_name()
    {
        var settings = SettingsFor("My.Endpoint", typeof(PlainSaga));
        var manifest = ManifestFor(settings);

        SqlSagaFeature.ConfigureSagaManifest(manifest, settings, new SqlDialect.MsSqlServer());

        Assert.That(manifest.Sagas.Single().TableName, Is.EqualTo("[dbo].[My_Endpoint_PlainSaga]"));
    }

    [Test]
    public void Saga_manifest_prefix_honours_custom_table_prefix()
    {
        var settings = SettingsFor("My.Endpoint", typeof(PlainSaga));
        settings.Set("SqlPersistence.TablePrefix", "Foo_");
        var manifest = ManifestFor(settings);

        SqlSagaFeature.ConfigureSagaManifest(manifest, settings, new SqlDialect.MsSqlServer());

        Assert.That(manifest.Sagas.Single().TableName, Is.EqualTo("[dbo].[Foo_PlainSaga]"));
    }

    static SagaMetadataCollection MetadataFor(params Type[] sagaTypes)
    {
        var metadata = new SagaMetadataCollection();
        metadata.AddRange(SagaMetadata.CreateMany(sagaTypes));
        return metadata;
    }

    static SettingsHolder SettingsFor(string endpointName, params Type[] sagaTypes)
    {
        var settings = new SettingsHolder();
        settings.Set("NServiceBus.Routing.EndpointName", endpointName);
        settings.Set(MetadataFor(sagaTypes));
        return settings;
    }

    // Mirrors how ManifestOutput.Defaults builds the manifest.
    static ManifestOutput.PersistenceManifest ManifestFor(IReadOnlySettings settings) =>
        new() { Prefix = settings.Get<string>("NServiceBus.Routing.EndpointName") };

    [SqlSaga(correlationProperty: "AttributeChosenProperty")]
    public class SagaWithOverriddenCorrelationProperty :
        Saga<SagaWithOverriddenCorrelationProperty.SagaData>, IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(saga => saga.MapperChosenProperty).ToMessage<StartMessage>(message => message.CorrelationProperty);

        public class SagaData : ContainSagaData
        {
            public string MapperChosenProperty { get; set; }
            public string AttributeChosenProperty { get; set; }
        }
    }

    [SqlSaga(tableSuffix: "ShortSagaTable")]
    public class SagaWithEntityNameLongerThanOracleAllows :
        Saga<SagaWithEntityNameLongerThanOracleAllows.SagaDataWithEntityNameLongerThanOracleAllows>,
        IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaDataWithEntityNameLongerThanOracleAllows> mapper) =>
            mapper.MapSaga(saga => saga.CorrelationProperty).ToMessage<StartMessage>(message => message.CorrelationProperty);

        public class SagaDataWithEntityNameLongerThanOracleAllows : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
        }
    }

    public class PlainSaga :
        Saga<PlainSaga.SagaData>, IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
            mapper.MapSaga(saga => saga.CorrelationProperty).ToMessage<StartMessage>(message => message.CorrelationProperty);

        public class SagaData : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
        }
    }

    public class StartMessage : IMessage
    {
        public string CorrelationProperty { get; set; }
    }
}
