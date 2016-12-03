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
public class MsmqTransportIntegrationTests
{
    static string connection = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
    static ManualResetEvent ManualResetEvent = new ManualResetEvent(false);
    string endpointName = "MsmqTransportIntegration";

    [SetUp]
    [TearDown]
    public void Setup()
    {
        MsmqQueueDeletion.DeleteQueuesForEndpoint(endpointName);
    }

    [Test]
    [TestCase(TransportTransactionMode.TransactionScope)]
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.None)]
    public async Task Write(TransportTransactionMode transactionMode)
    {
        var sagaDefinition = new SagaDefinition(
            tableSuffix: nameof(Saga1),
            name: nameof(Saga1),
            correlationProperty: new  CorrelationProperty
            (
                name: nameof(Saga1.SagaData.StartId),
                type: CorrelationPropertyType.Guid
            )
        );

        Execute(SagaScriptBuilder.BuildDropScript(sagaDefinition, SqlVarient.MsSqlServer));
        Execute(SagaScriptBuilder.BuildCreateScript(sagaDefinition, SqlVarient.MsSqlServer));
        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(endpointName);
        var typesToScan = TypeScanner.NestedTypes<MsmqTransportIntegrationTests>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        var transport = endpointConfiguration.UseTransport<MsmqTransport>();
        transport.Transactions(transactionMode);
        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.ConnectionString(connection);
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
        using (var sqlConnection = new SqlConnection(connection))
        {
            sqlConnection.Open();
            using (var command = sqlConnection.CreateCommand())
            {
                command.CommandText = script;
                command.AddParameter("schema", "dbo");
                command.AddParameter("endpointName", $"{endpointName}.");
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
            mapper.MapMessage<StartSagaMessage>(message => message.StartId);
        }

    }

    class MessageToReply : IMessage
    {
    }

}