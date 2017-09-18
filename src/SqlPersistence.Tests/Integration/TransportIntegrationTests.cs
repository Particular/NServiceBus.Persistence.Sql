using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class TransportIntegrationTests : IDisposable
{
    static ManualResetEvent ManualResetEvent = new ManualResetEvent(false);
    //string endpointName = "TransportIntegrationTests";
    BuildSqlDialect sqlDialect = BuildSqlDialect.MsSqlServer;
    SqlConnection dbConnection;
    SagaDefinition sagaDefinition;

    public TransportIntegrationTests()
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
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(sagaDefinition, sqlDialect), nameof(TransportIntegrationTests));
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaDefinition, sqlDialect), nameof(TransportIntegrationTests));
        dbConnection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlDialect), nameof(TransportIntegrationTests));
        dbConnection.ExecuteCommand(TimeoutScriptBuilder.BuildCreateScript(sqlDialect), nameof(TransportIntegrationTests));
    }

    [TearDown]
    public void TearDown()
    {
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(sagaDefinition, sqlDialect), nameof(TransportIntegrationTests));
        dbConnection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlDialect), nameof(TransportIntegrationTests));
    }

    [Test]
    [TestCase(TransportTransactionMode.TransactionScope)]
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.None)]
    public async Task Write(TransportTransactionMode transactionMode)
    {
        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(nameof(TransportIntegrationTests));
        var typesToScan = TypeScanner.NestedTypes<TransportIntegrationTests>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        transport.ConnectionString(MsSqlConnectionBuilder.ConnectionString);
        transport.Transactions(transactionMode);
        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        persistence.ConnectionBuilder(MsSqlConnectionBuilder.Build);
        persistence.DisableInstaller();
        persistence.SubscriptionSettings().DisableCache();

        var endpoint = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
        var startSagaMessage = new StartSagaMessage
        {
            StartId = Guid.NewGuid()
        };
        await endpoint.SendLocal(startSagaMessage).ConfigureAwait(false);
        Assert.IsTrue(ManualResetEvent.WaitOne(TimeSpan.FromSeconds(30)));
        await endpoint.Stop().ConfigureAwait(false);
    }

    public class StartSagaMessage : IMessage
    {
        public Guid StartId { get; set; }
    }

    public class TimeoutMessage : IMessage
    {
    }

    public class Saga1 : SqlSaga<Saga1.SagaData>,
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
            ManualResetEvent.Set();
            return Task.FromResult(0);
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

    class MessageToReply : IMessage
    {
    }

    public void Dispose()
    {
        dbConnection?.Dispose();
    }
}