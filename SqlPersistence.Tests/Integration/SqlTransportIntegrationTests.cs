using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using SqlVarient = NServiceBus.Persistence.Sql.ScriptBuilder.SqlVarient;

[TestFixture]
public class SqlTransportIntegrationTests
{
    string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
    static ManualResetEvent ManualResetEvent = new ManualResetEvent(false);
    string endpointName = "SqlTransportIntegration";

    [SetUp]
    [TearDown]
    public void Setup()
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            SqlQueueDeletion.DeleteQueuesForEndpoint(connection ,"dbo",endpointName);
        }
    }

    [Test]
    [TestCase(TransportTransactionMode.TransactionScope)]
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.None)]
    public async Task SmokeTest(TransportTransactionMode transactionMode)
    {
        var sagaDefinition = new SagaDefinition(
            tableSuffix : "Saga1",
            name : "Saga1",
            correlationProperty : new CorrelationProperty
            (
                name: "StartId",
                type:  CorrelationPropertyType.Guid
            )
        );

        Execute(SagaScriptBuilder.BuildDropScript(sagaDefinition, SqlVarient.MsSqlServer));
        Execute(SagaScriptBuilder.BuildCreateScript(sagaDefinition, SqlVarient.MsSqlServer));
        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(endpointName);
        var typesToScan = TypeScanner.NestedTypes<SqlTransportIntegrationTests>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        transport.Transactions(transactionMode);
        transport.ConnectionString(connectionString);
        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.ConnectionString(connectionString);
        persistence.DisableInstaller();

        var endpoint = await Endpoint.Start(endpointConfiguration);
        var startSagaMessage = new StartSagaMessage
        {
            StartId = Guid.NewGuid()
        };
        await endpoint.SendLocal(startSagaMessage);
        ManualResetEvent.WaitOne();
        await endpoint.Stop();
    }


    void Execute(string script)
    {
        using (var sqlConnection = new SqlConnection(connectionString))
        {
            sqlConnection.Open();
            using (var command = sqlConnection.CreateCommand())
            {
                command.CommandText = script;
                command.AddParameter("schema", "dbo");
                command.AddParameter("tablePrefix", $"{endpointName}.");
                command.ExecuteNonQuery();
            }
        }
    }
    public class StartSagaMessage : IMessage
    {
        public Guid StartId { get; set; }
    }

    public class TimeoutMessage : IMessage
    {
    }

    [SqlSaga(
         correlationProperty: nameof(SagaData.StartId)
     )]
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
            return Task.CompletedTask;
        }

        public class SagaData : ContainSagaData
        {
            public Guid StartId { get; set; }
        }

        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
        {
            mapper.MapMessage<StartSagaMessage>(m => m.StartId);
        }

    }

    class MessageToReply : IMessage
    {
    }

}