using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.SqlServerXml;
using NUnit.Framework;

[TestFixture]
public class SqlTransportIntegrationTests
{
    static string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
    static ManualResetEvent ManualResetEvent = new ManualResetEvent(false);
    string endpointName = "SqlTransportIntegration";

    [SetUp]
    [TearDown]
    public void Setup()
    {
        QueueDeletion.DeleteQueuesForEndpoint(endpointName);
    }

    [Test]
    [TestCase(TransportTransactionMode.TransactionScope)]
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.None)]
    public async Task Write(TransportTransactionMode transactionMode)
    {
        var sagaDefinition = new SagaDefinition
        {
            Name = SagaTableNameBuilder.GetTableSuffix(typeof(Saga1))
        };
        await DbBuilder.ReCreate(connectionString, endpointName, sagaDefinition);
        var endpointConfiguration = new EndpointConfiguration(endpointName);
        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.EnableInstallers();
        var typesToScan = TypeScanner.NestedTypes<SqlTransportIntegrationTests>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        transport.Transactions(transactionMode);
        transport.ConnectionString(connectionString);
        var persistence = endpointConfiguration.UsePersistence<SqlXmlPersistence>();
        persistence.ConnectionString(connectionString);
        persistence.DisableInstaller();

        var endpoint = await Endpoint.Start(endpointConfiguration);
        await endpoint.SendLocal(new StartSagaMessage());
        ManualResetEvent.WaitOne();
        await endpoint.Stop();
    }

    public class StartSagaMessage : IMessage
    {
    }

    public class TimeoutMessage : IMessage
    {
    }

    public class Saga1 : XmlSaga<Saga1.SagaData>,
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

        public class SagaData : XmlSagaData
        {
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
        }

    }


    class MessageToReply : IMessage
    {
    }

}