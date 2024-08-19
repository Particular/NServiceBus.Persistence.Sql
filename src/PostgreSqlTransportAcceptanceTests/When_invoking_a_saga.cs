namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Persistence.Sql;
    using Pipeline;

    public class When_invoking_a_saga : NServiceBusAcceptanceTest
    {
        [Test]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive, false)] //Uses shared DbConnection/DbTransaction to ensure exactly-once
        [TestCase(TransportTransactionMode.ReceiveOnly, true)] //Uses the Outbox to ensure exactly-once
        public async Task Should_rollback_saga_data_changes_when_transport_transaction_is_rolled_back(TransportTransactionMode transactionMode, bool enableOutbox)
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointThatHostsASaga>(
                    b => b.When((session, ctx) => session.SendLocal(new SagaMessage()
                    {
                        Id = ctx.TestRunId
                    })).DoNotFailOnErrorMessages().CustomConfig(c =>
                    {
                        c.ConfigureTransport().TransportTransactionMode = transactionMode;
                        if (enableOutbox)
                        {
                            c.EnableOutbox();
                        }
                    }))
                .Done(c => c.ReplyReceived)
                .Run();

            Assert.True(context.ReplyReceived);
            Assert.That(context.TransactionEscalatedToDTC, Is.False);
            Assert.AreEqual(2, context.SagaInvocationCount, "Saga handler should be called twice");
            Assert.AreEqual(1, context.SagaCounterValue, "Saga value should be incremented only once");
        }

        public class Context : ScenarioContext
        {
            public int SagaCounterValue { get; set; }
            public int SagaInvocationCount { get; set; }
            public bool TransactionEscalatedToDTC { get; set; }
            public bool ReplyReceived { get; set; }
        }

        public class EndpointThatHostsASaga : EndpointConfigurationBuilder
        {
            public EndpointThatHostsASaga()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.Pipeline.Register(new BehaviorThatThrowsAfterFirstMessage.Registration());
                    var recoverability = config.Recoverability();
                    recoverability.Immediate(settings =>
                    {
                        settings.NumberOfRetries(1);
                    });
                });
            }

            public class TestSaga : SqlSaga<TestSaga.SagaData>, IAmStartedByMessages<SagaMessage>
            {
                Context testContext;

                public TestSaga(Context context)
                {
                    testContext = context;
                }

                public async Task Handle(SagaMessage message, IMessageHandlerContext context)
                {
                    if (message.Id != testContext.TestRunId)
                    {
                        return;
                    }

                    Data.TestRunId = message.Id;
                    Data.Counter += 1;

                    testContext.SagaCounterValue = Data.Counter;
                    testContext.SagaInvocationCount++;

                    await context.SendLocal(new ReplyMessage
                    {
                        Id = message.Id
                    });
                }

                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.ConfigureMapping<SagaMessage>(m => m.Id);
                }

                protected override string CorrelationPropertyName => "TestRunId";
                public class SagaData : ContainSagaData
                {
                    public virtual Guid TestRunId { get; set; }
                    public virtual int Counter { get; set; }
                }
            }

            public class Handler : IHandleMessages<ReplyMessage>
            {
                Context testContext;

                public Handler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(ReplyMessage message, IMessageHandlerContext context)
                {
                    if (testContext.TestRunId == message.Id)
                    {
                        testContext.ReplyReceived = true;
                    }

                    return Task.CompletedTask;
                }
            }

            class BehaviorThatThrowsAfterFirstMessage : Behavior<IIncomingLogicalMessageContext>
            {
                Context testContext;

                public BehaviorThatThrowsAfterFirstMessage(Context context)
                {
                    testContext = context;
                }

                public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    await next();

                    if (testContext.SagaInvocationCount == 1)
                    {
                        testContext.TransactionEscalatedToDTC = Transaction.Current.TransactionInformation.DistributedIdentifier != Guid.Empty;

                        throw new SimulatedException();
                    }
                }

                public class Registration : RegisterStep
                {
                    public Registration() : base("BehaviorThatThrowsAfterFirstMessage", typeof(BehaviorThatThrowsAfterFirstMessage), "BehaviorThatThrowsAfterFirstMessage")
                    {
                    }
                }
            }
        }

        public class SagaMessage : IMessage
        {
            public Guid Id { get; set; }
        }

        public class ReplyMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}