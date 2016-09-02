using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.SqlServerXml;
using NServiceBus.Pipeline;
using NUnit.Framework;

[TestFixture]
public class SagaConsistencyTests
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
            await SqlQueueDeletion.DeleteQueuesForEndpoint(sqlConnection, "dbo", endpointName);
        }
    }

    [Test]
    public async Task In_DTC_mode_enlists_in_the_ambient_transaction()
    {
        await RunTest(e =>
        {
            e.UseTransport<MsmqTransport>().Transactions(TransportTransactionMode.TransactionScope);
        });
    }

    [Test]
    public async Task In_DTC_SqlTransport_mode_does_not_escalate()
    {
        await RunTest(e =>
        {
            e.UseTransport<SqlServerTransport>().Transactions(TransportTransactionMode.TransactionScope).ConnectionString(connectionString);
            e.Pipeline.Register(new EscalationChecker(), "EscalationChecker");
        });
    }

    [Test]
    public async Task In_native_SqlTransport_mode_enlists_in_native_transaction()
    {
        await RunTest(e =>
        {            
            e.UseTransport<SqlServerTransport>().Transactions(TransportTransactionMode.SendsAtomicWithReceive).ConnectionString(connectionString);
        });
    }

    [Test]
    public async Task In_outbox_mode_enlists_in_outbox_transaction()
    {
        await RunTest(e =>
        {
            e.GetSettings().Set("DisableOutboxTransportCheck", true);
            e.UseTransport<MsmqTransport>();
            e.EnableOutbox();
        });
    }

    async Task RunTest(Action<EndpointConfiguration> testCase)
    {
        ManualResetEvent.Reset();
        string message = null;
        var sagaDefinition = new SagaDefinition
        {
            Name = SagaTableNameBuilder.GetTableSuffix(typeof(Saga1))
        };
        await DbBuilder.ReCreate(connectionString, endpointName, sagaDefinition);
        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(endpointName);
        var typesToScan = TypeScanner.NestedTypes<SagaConsistencyTests>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        testCase(endpointConfiguration);
        transport.ConnectionString(connectionString);
        var persistence = endpointConfiguration.UsePersistence<SqlXmlPersistence>();
        persistence.ConnectionString(connectionString);
        persistence.DisableInstaller();
        endpointConfiguration.DefineCriticalErrorAction(c =>
        {
            message = c.Error;
            ManualResetEvent.Set();
            return Task.FromResult(0);
        });
        endpointConfiguration.LimitMessageProcessingConcurrencyTo(1);
        endpointConfiguration.Pipeline.Register(new FailureTrigger(), "Failure trigger");

        var endpoint = await Endpoint.Start(endpointConfiguration);
        var sagaId = Guid.NewGuid();
        await endpoint.SendLocal(new StartSagaMessage
        {
            SagaId = sagaId
        });
        await endpoint.SendLocal(new FailingMessage
        {
            SagaId = sagaId
        });
        await endpoint.SendLocal(new CheckMessage
        {
            SagaId = sagaId
        });
        ManualResetEvent.WaitOne();
        await endpoint.Stop();

        Assert.AreEqual("Success", message);
    }

    class FailureTrigger : Behavior<IIncomingLogicalMessageContext>
    {
        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            await next();
            if (context.Message.Instance is FailingMessage)
            {
                throw new Exception("Boom!");
            }


        }
    }

    class EscalationChecker : Behavior<IInvokeHandlerContext>
    {
        public override Task Invoke(IInvokeHandlerContext context, Func<Task> next)
        {
            var tx = Transaction.Current;
            Assert.AreEqual(Guid.Empty, tx.TransactionInformation.DistributedIdentifier);
            return next();
        }
    }

    public class StartSagaMessage : IMessage
    {
        public Guid SagaId { get; set; }
    }

    public class FailingMessage : IMessage
    {
        public Guid SagaId { get; set; }
    }

    public class CheckMessage : IMessage
    {
        public Guid SagaId { get; set; }
    }

    public class Saga1 : XmlSaga<Saga1.SagaData>,
        IAmStartedByMessages<StartSagaMessage>,
        IHandleMessages<FailingMessage>,
        IHandleMessages<CheckMessage>
    {
        public CriticalError CriticalError { get; set; }

        public class SagaData : ContainSagaData
        {
            [CorrelationIdAttribute]
            public Guid CorrelationId { get; set; }
            public bool PersistedFailingMessageResult { get; set; }
        }

        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
        {
            mapper.MapMessage<StartSagaMessage>(m => m.SagaId);
            mapper.MapMessage<FailingMessage>(m => m.SagaId);
            mapper.MapMessage<CheckMessage>(m => m.SagaId);
        }
        public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
        {
            return Task.FromResult(0);
        }

        public Task Handle(FailingMessage message, IMessageHandlerContext context)
        {
            Data.PersistedFailingMessageResult = true;
            return Task.FromResult(0);
        }

        public Task Handle(CheckMessage message, IMessageHandlerContext context)
        {
            if (Data.PersistedFailingMessageResult)
            {
                CriticalError.Raise("Failure", new Exception());
            }
            else
            {
                CriticalError.Raise("Success", new Exception());
            }
            return Task.FromResult(0);
        }
    }
}