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
        var metadata = SagaMetadata.CreateMany([typeof(SagaWithEntityNameLongerThanOracleAllows)]).Single();

        var manifest = SqlSagaFeature.CreateSagaManifest(
            metadata,
            new SqlDialect.Oracle(),
            "Endpoint_",
            sagaName => sagaName);

        Assert.That(manifest.TableName, Is.EqualTo("\"SHORTSAGATABLE\""));
        Assert.That(manifest.Indexes.Single().Name, Is.EqualTo("Index_Correlation_CorrelationProperty"));
        Assert.That(manifest.Indexes.Single().Columns, Is.EqualTo("CorrelationProperty"));
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

    public class StartMessage : IMessage
    {
        public string CorrelationProperty { get; set; }
    }
}
