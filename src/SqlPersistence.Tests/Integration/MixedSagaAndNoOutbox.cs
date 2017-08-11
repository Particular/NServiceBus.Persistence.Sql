using System;
using System.Data.SqlClient;
using System.Threading;
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
    static ManualResetEvent manualResetEvent;

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
        manualResetEvent = new ManualResetEvent(false);
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(sagaDefinition, sqlVariant), nameof(MixedSagaAndNoOutbox));
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaDefinition, sqlVariant), nameof(MixedSagaAndNoOutbox));
        dbConnection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlVariant), nameof(MixedSagaAndNoOutbox));
        dbConnection.ExecuteCommand(TimeoutScriptBuilder.BuildCreateScript(sqlVariant), nameof(MixedSagaAndNoOutbox));
    }

    [TearDown]
    public void TearDown()
    {
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(sagaDefinition, sqlVariant), nameof(MixedSagaAndNoOutbox));
        dbConnection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlVariant), nameof(MixedSagaAndNoOutbox));
    }

    [Test]
    public async Task RunSqlPrimary()
    {
        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(nameof(MixedSagaAndNoOutbox));
        var typesToScan = TypeScanner.NestedTypes<MixedSagaAndNoOutbox>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        endpointConfiguration.UseTransport<LearningTransport>();

        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        persistence.ConnectionBuilder(MsSqlConnectionBuilder.Build);
        persistence.DisableInstaller();
        endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.Sagas>();

        var endpoint = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
        var startSagaMessage = new StartSagaMessage
        {
            StartId = Guid.NewGuid()
        };
        await endpoint.SendLocal(startSagaMessage).ConfigureAwait(false);
        Assert.IsTrue(manualResetEvent.WaitOne(TimeSpan.FromSeconds(30)));
        await endpoint.Stop().ConfigureAwait(false);
    }

    [Test]
    [Explicit("Core mem persistence does not allow this yet")]
    public async Task RunSqlSecondary()
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
        Assert.IsTrue(manualResetEvent.WaitOne(TimeSpan.FromSeconds(30)));
        await endpoint.Stop().ConfigureAwait(false);
    }

    public class StartSagaMessage : IMessage
    {
        public Guid StartId { get; set; }
    }

    public class TimeoutMessage : IMessage
    {
    }

    public class Saga1 : Saga<Saga1.SagaData>,
        IAmStartedByMessages<StartSagaMessage>,
        IHandleTimeouts<TimeoutMessage>
    {
        public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
        {
            return RequestTimeout<TimeoutMessage>(context, TimeSpan.FromMilliseconds(100));
        }

        public Task Timeout(TimeoutMessage state, IMessageHandlerContext context)
        {
            MarkAsComplete();
            manualResetEvent.Set();
            return Task.FromResult(0);
        }

        public class SagaData : ContainSagaData
        {
            public Guid StartId { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<StartSagaMessage>(message => message.StartId)
                .ToSaga(data => data.StartId);
        }
    }

    public void Dispose()
    {
        dbConnection?.Dispose();
    }
}