using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class MixedSagaAndNoOutbox : IDisposable
{
    BuildSqlVariant sqlVariant = BuildSqlVariant.MsSqlServer;
    SqlConnection dbConnection;
    SagaDefinition sagaDefinition;

    public MixedSagaAndNoOutbox()
    {
        dbConnection = MsSqlConnectionBuilder.Build();
        dbConnection.Open();
        sagaDefinition = new SagaDefinition(
            tableSuffix: nameof(Saga1),
            name: nameof(Saga1),
            correlationProperty: new CorrelationProperty
            (
                name: nameof(Saga1.SagaData.StartId),
                type: CorrelationPropertyType.Guid
            )
        );
    }

    [SetUp]
    public void Setup()
    {
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(sagaDefinition, sqlVariant), nameof(MixedSagaAndNoOutbox));
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaDefinition, sqlVariant), nameof(MixedSagaAndNoOutbox));
    }

    [TearDown]
    public void TearDown()
    {
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(sagaDefinition, sqlVariant), nameof(MixedSagaAndNoOutbox));
    }

    [Test]
    public async Task Run()
    {
        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(nameof(MixedSagaAndNoOutbox));
        var typesToScan = TypeScanner.NestedTypes<MixedSagaAndNoOutbox>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        endpointConfiguration.UseTransport<LearningTransport>();

        endpointConfiguration.UsePersistence<InMemoryPersistence>();
        var persistence = endpointConfiguration.UsePersistence<SqlPersistence, StorageType.Sagas>();
        persistence.ConnectionBuilder(MsSqlConnectionBuilder.Build);
        persistence.DisableInstaller();

        var endpoint = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
        var startSagaMessage = new StartSagaMessage
        {
            StartId = Guid.NewGuid()
        };
        await endpoint.SendLocal(startSagaMessage).ConfigureAwait(false);
        await endpoint.Stop().ConfigureAwait(false);
    }

    public class StartSagaMessage : IMessage
    {
        public Guid StartId { get; set; }
    }

    public class Saga1 : SqlSaga<Saga1.SagaData>,
        IAmStartedByMessages<StartSagaMessage>
    {
        public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }

        public class SagaData : ContainSagaData
        {
            public Guid StartId { get; set; }
        }

        protected override string CorrelationPropertyName => nameof(SagaData.StartId);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
            mapper.ConfigureMapping<StartSagaMessage>(message => message.StartId);
        }
    }

    public void Dispose()
    {
        dbConnection?.Dispose();
    }
}