using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Sagas;
using NUnit.Framework;

public class SqlServerSagaDataParameterTests
{
    [Test]
    public void SmallSagaAfterLargeSagaBindsOnlyItsOwnData()
    {
        var dialect = new SqlDialect.MsSqlServer();
        var sagaInfo = BuildSagaInfo(dialect);

        using (var command = new CommandWrapper(new SqlCommand(), dialect))
        {
            dialect.BuildSagaData(command, sagaInfo, new TheSaga.SagaData { Payload = new string('x', 100_000) });
        }

        var smallSaga = new TheSaga.SagaData { Payload = "small" };
        using (var command = new CommandWrapper(new SqlCommand(), dialect))
        {
            var parameter = new SqlParameter();
            dialect.SetJsonParameterValue(parameter, dialect.BuildSagaData(command, sagaInfo, smallSaga));

            _ = parameter.DbType;

            Assert.That(parameter.Value, Is.EqualTo(sagaInfo.ToJson(smallSaga)));
        }
    }

    static RuntimeSagaInfo BuildSagaInfo(SqlDialect dialect)
    {
        var metadata = new SagaMetadataCollection();
        metadata.Initialize([typeof(TheSaga)]);

        var infoCache = new SagaInfoCache(
            versionSpecificSettings: null,
            jsonSerializer: Serializer.JsonSerializer,
            readerCreator: reader => new JsonTextReader(reader),
            writerCreator: writer => new JsonTextWriter(writer),
            tablePrefix: "Test_",
            sqlDialect: dialect,
            metadataCollection: metadata,
            nameFilter: sagaName => sagaName);

        return infoCache.GetInfo(typeof(TheSaga.SagaData));
    }

    public class TheSaga : Saga<TheSaga.SagaData>, IAmStartedByMessages<StartSaga>
    {
        public class SagaData : ContainSagaData
        {
            public string Key { get; set; }
            public string Payload { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.Key).ToMessage<StartSaga>(m => m.Key);

        public Task Handle(StartSaga message, IMessageHandlerContext context) => Task.CompletedTask;
    }

    public class StartSaga : IMessage
    {
        public string Key { get; set; }
    }
}