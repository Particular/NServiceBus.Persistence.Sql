using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;

[TestFixture]
public class SqlTransportIntegrationTests
{
    static string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
    static ManualResetEvent ManualResetEvent = new ManualResetEvent(false);
    string endpointName = "SqlTransportIntegration";

    [SetUp]
    [TearDown]
    public async Task Setup()
    {
        using (var sqlConnection = await SqlHelpers.New(connectionString))
        {
            await SqlQueueDeletion.DeleteQueuesForEndpoint(sqlConnection ,"dbo",endpointName);
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
            {
                Name = "StartId",
                Type =  CorrelationMemberType.Guid
            }
        );
        await DbBuilder.ReCreate(connectionString, endpointName, sagaDefinition);
        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(endpointName);
        var typesToScan = TypeScanner.NestedTypes<SqlTransportIntegrationTests>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        transport.Transactions(transactionMode);
        transport.ConnectionString(connectionString);
        var persistence = endpointConfiguration.UsePersistence<SqlXmlPersistence>();
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
            ManualResetEvent.Set();
            return Task.FromResult(0);
        }

        public class SagaData : ContainSagaData
        {
            public Guid StartId { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<StartSagaMessage>(m => m.StartId)
                .ToSaga(data => data.StartId);
        }
    }

    class MessageToReply : IMessage
    {
    }

}