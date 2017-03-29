namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Persistence.Sql;

    public class When_creating_transaction_scope_in_the_pipeline : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_adapt_that_scope_when_creating_synchronized_session_if_transport_is_in_native_mode()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b
                    .When(async session =>
                    {
                        var id = Guid.NewGuid();
                        await session.SendLocal(new StartSagaMessage
                        {
                            SomeId = id
                        });
                        await session.SendLocal(new StartSagaMessage
                        {
                            SomeId = id
                        });
                        await session.SendLocal(new StartSagaMessage
                        {
                            SomeId = id
                        });
                    }).DoNotFailOnErrorMessages())
                .Done(c => c.SagaDataPersisted)
                .Run();

            Assert.IsTrue(context.SagaDataPersisted);
        }

        public class Context : ScenarioContext
        {
            public bool SagaDataPersisted { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<MsmqTransport>().Transactions(TransportTransactionMode.SendsAtomicWithReceive);
                    c.LimitMessageProcessingConcurrencyTo(1);
                    c.Pipeline.Register(new TransactionScopeBehavior(), "Creates a new transaction scope");
                    c.Pipeline.Register(new SimulateFailureBehavior(), "Simulates failure before committing transport transaction");
                });
            }

            class TransactionScopeBehavior : Behavior<IIncomingLogicalMessageContext>
            {
                public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await next().ConfigureAwait(false);
                        scope.Complete();
                    }
                }
            }

            class SimulateFailureBehavior : Behavior<IIncomingPhysicalMessageContext>
            {
                public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
                {
                    await next().ConfigureAwait(false);
                    throw new SimulatedException();
                }
            }

            public class TestSaga13 : SqlSaga<TestSagaData13>, IAmStartedByMessages<StartSagaMessage>
            {
                protected override string CorrelationPropertyName => nameof(TestSagaData13.SomeId);

                public Context TestContext { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Data.Counter++;
                    if (Data.Counter == 3)
                    {
                        TestContext.SagaDataPersisted = true;
                    }
                    return Task.FromResult(0);
                }

                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId);
                }
            }

            public class TestSagaData13 : ContainSagaData
            {
                public Guid SomeId { get; set; }
                public int Counter { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}