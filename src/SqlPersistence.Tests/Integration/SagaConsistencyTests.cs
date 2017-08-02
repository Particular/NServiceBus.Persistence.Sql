//TODO: re enable when sql transport is updated

/**
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Pipeline;
using NUnit.Framework;

[TestFixture]
public class SagaConsistencyTests
{
    static ManualResetEvent ManualResetEvent = new ManualResetEvent(false);
    string endpointName = "SqlTransportIntegration";

    [SetUp]
    [TearDown]
    public void Setup()
    {
        using (var connection = MsSqlConnectionBuilder.Build())
        {
            connection.Open();
            SqlQueueDeletion.DeleteQueuesForEndpoint(connection, "dbo", endpointName);
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
            transport.ConnectionString(MsSqlConnectionBuilder.ConnectionString);
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
            transport.ConnectionString(MsSqlConnectionBuilder.ConnectionString);
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
            (
                name: nameof(Saga1.SagaData.CorrelationId),
                type: CorrelationPropertyType.Guid
            )
        );

        Execute(SagaScriptBuilder.BuildDropScript(sagaDefinition, BuildSqlVariant.MsSqlServer));
        Execute(OutboxScriptBuilder.BuildDropScript(BuildSqlVariant.MsSqlServer));
        Execute(SagaScriptBuilder.BuildCreateScript(sagaDefinition, BuildSqlVariant.MsSqlServer));
        Execute(OutboxScriptBuilder.BuildCreateScript(BuildSqlVariant.MsSqlServer));
        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(endpointName);
        var typesToScan = TypeScanner.NestedTypes<SagaConsistencyTests>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        endpointConfiguration.DisableFeature<NServiceBus.Features.TimeoutManager>();
        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        testCase(endpointConfiguration);
        transport.ConnectionString(MsSqlConnectionBuilder.ConnectionString);
        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(MsSqlConnectionBuilder.Build);
        persistence.SubscriptionSettings().DisableCache();
        persistence.DisableInstaller();
        endpointConfiguration.DefineCriticalErrorAction(c =>
        {
            message = c.Error;
            ManualResetEvent.Set();
            return Task.FromResult(0);
        });
        endpointConfiguration.LimitMessageProcessingConcurrencyTo(1);
        endpointConfiguration.Pipeline.Register(new FailureTrigger(), "Failure trigger");

        var endpoint = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
        var sagaId = Guid.NewGuid();
        await endpoint.SendLocal(new StartSagaMessage
        {
            SagaId = sagaId
        }).ConfigureAwait(false);
        await endpoint.SendLocal(new FailingMessage
        {
            SagaId = sagaId
        }).ConfigureAwait(false);
        await endpoint.SendLocal(new CheckMessage
        {
            SagaId = sagaId
        }).ConfigureAwait(false);
        ManualResetEvent.WaitOne();
        await endpoint.Stop().ConfigureAwait(false);

        Assert.AreEqual("Success", message);
    }


    void Execute(string script)
    {
        using (var sqlConnection = MsSqlConnectionBuilder.Build())
        {
            sqlConnection.Open();
            using (var command = sqlConnection.CreateCommand())
            {
                command.CommandText = script;
                command.AddParameter("tablePrefix", $"{endpointName}_");
                command.AddParameter("schema", "dbo");
                command.ExecuteNonQuery();
            }
        }
    }

    class FailureTrigger : Behavior<IIncomingLogicalMessageContext>
    {
        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);
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

    public class Saga1 : SqlSaga<Saga1.SagaData>,
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

        protected override string CorrelationPropertyName => nameof(SagaData.CorrelationId);

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
            mapper.ConfigureMapping<StartSagaMessage>(m => m.SagaId);
            mapper.ConfigureMapping<FailingMessage>(m => m.SagaId);
            mapper.ConfigureMapping<CheckMessage>(m => m.SagaId);
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
**/