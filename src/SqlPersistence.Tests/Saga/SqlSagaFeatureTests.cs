using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;
using NUnit.Framework;

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
    public void Saga_manifest_applies_name_filter_to_table_suffix()
    {
        // The filter runs against the table suffix, not the saga name, matching the runtime.
        var manifest = SqlSagaFeature.BuildSagaManifests(
            MetadataFor(typeof(SagaWithEntityNameLongerThanOracleAllows)),
            new SqlDialect.MsSqlServer(),
            "Endpoint_",
            sagaName => sagaName == "ShortSagaTable" ? "Filtered" : sagaName).Single();

        Assert.That(manifest.TableName, Is.EqualTo("[dbo].[Endpoint_Filtered]"));
    }

    static SagaMetadataCollection MetadataFor(params Type[] sagaTypes)
    {
        var metadata = new SagaMetadataCollection();
        metadata.AddRange(SagaMetadata.CreateMany(sagaTypes));
        return metadata;
    }

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
