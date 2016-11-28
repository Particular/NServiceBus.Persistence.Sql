using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;
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
    public Task In_DTC_mode_enlists_in_the_ambient_transaction()
    {
        return RunTest(e =>
        {
            var transport = e.UseTransport<MsmqTransport>();
            transport.Transactions(TransportTransactionMode.TransactionScope);
        });
    }

    [Test]
    public Task In_DTC_SqlTransport_mode_does_not_escalate()
    {
        return RunTest(e =>
        {
            var transport = e.UseTransport<SqlServerTransport>();
            transport.Transactions(TransportTransactionMode.TransactionScope);
            transport.ConnectionString(connectionString);
            e.Pipeline.Register(new EscalationChecker(), "EscalationChecker");
        });
    }

    [Test]
    public Task In_native_SqlTransport_mode_enlists_in_native_transaction()
    {
        return RunTest(e =>
        {
            var transport = e.UseTransport<SqlServerTransport>();
            transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
            transport.ConnectionString(connectionString);
        });
    }

    [Test]
    public Task In_outbox_mode_enlists_in_outbox_transaction()
    {
        return RunTest(e =>
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
        var sagaDefinition = new SagaDefinition(
            tableSuffix: nameof(Saga1),
            name: nameof(Saga1),
            correlationProperty: new CorrelationProperty
            {
                Name = nameof(Saga1.SagaData.CorrelationId),
                Type = CorrelationMemberType.Guid
            }
        );
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

    [SqlSaga(
         correlationProperty: nameof(SagaData.CorrelationId)
     )]
    public class Saga1 : Saga<Saga1.SagaData>,
        IAmStartedByMessages<StartSagaMessage>,
        IHandleMessages<FailingMessage>,
        IHandleMessages<CheckMessage>
    {
        public CriticalError CriticalError { get; set; }

        public class SagaData : ContainSagaData
        {
            public Guid CorrelationId { get; set; }
            public bool PersistedFailingMessageResult { get; set; }
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<StartSagaMessage>(m => m.SagaId)
                .ToSaga(data => data.CorrelationId);
            mapper.ConfigureMapping<FailingMessage>(m => m.SagaId)
                .ToSaga(data => data.CorrelationId);
            mapper.ConfigureMapping<CheckMessage>(m => m.SagaId)
                .ToSaga(data => data.CorrelationId);
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