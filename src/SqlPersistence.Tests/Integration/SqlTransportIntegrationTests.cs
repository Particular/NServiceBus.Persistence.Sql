//TODO: add tests for the new option to not reuse the connection

using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Transport.SQLServer;
using NUnit.Framework;

[TestFixture]
public class SqlTransportIntegrationTests : IDisposable
{
    static ManualResetEvent ManualResetEvent = new ManualResetEvent(false);
    BuildSqlDialect sqlVariant = BuildSqlDialect.MsSqlServer;
    SqlConnection dbConnection;
    SagaDefinition sagaDefinition;

    public SqlTransportIntegrationTests()
    {
        dbConnection = MsSqlConnectionBuilder.Build();
        dbConnection.Open();
        sagaDefinition = new SagaDefinition(
            tableSuffix: "Saga1",
            name: "Saga1",
            correlationProperty: new CorrelationProperty
            (
                name: "StartId",
                type: CorrelationPropertyType.Guid
            )
        );
    }

    [SetUp]
    public void Setup()
    {
        SqlQueueDeletion.DeleteQueuesForEndpoint(dbConnection, "dbo", nameof(SqlTransportIntegrationTests));
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(sagaDefinition, sqlVariant), nameof(SqlTransportIntegrationTests));
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaDefinition, sqlVariant), nameof(SqlTransportIntegrationTests));

        dbConnection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlVariant), nameof(SqlTransportIntegrationTests));
        dbConnection.ExecuteCommand(TimeoutScriptBuilder.BuildCreateScript(sqlVariant), nameof(SqlTransportIntegrationTests));
    }

    [TearDown]
    public void TearDown()
    {
        SqlQueueDeletion.DeleteQueuesForEndpoint(dbConnection, "dbo", nameof(SqlTransportIntegrationTests));
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(sagaDefinition, sqlVariant), nameof(SqlTransportIntegrationTests));
        dbConnection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlVariant), nameof(SqlTransportIntegrationTests));
    }

    [Test]
#if NET452
    [TestCase(TransportTransactionMode.TransactionScope)]
#endif
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.None)]
    public async Task SmokeTest(TransportTransactionMode transactionMode)
    {
        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(nameof(SqlTransportIntegrationTests));
        var typesToScan = TypeScanner.NestedTypes<SqlTransportIntegrationTests>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        transport.Transactions(transactionMode);
        transport.UseCustomSqlConnectionFactory(async () =>
        {
            var connection = MsSqlConnectionBuilder.Build();
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        });
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
        ManualResetEvent.WaitOne();
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
            mapper.ConfigureMapping<StartSagaMessage>(m => m.StartId);
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