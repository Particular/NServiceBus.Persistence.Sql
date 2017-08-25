namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Persistence.Sql;

    public class When_creating_new_transaction_scope_in_the_pipeline : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_fail_when_creating_synchronized_storage_session()
        {
            Requires.DtcSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b
                    .When(async session =>
                    {
                        var id = Guid.NewGuid();
                        await session.SendLocal(new StartSagaMessage
                        {
                            SomeId = id
                        }).ConfigureAwait(false);
                    }).DoNotFailOnErrorMessages())
                .Done(c => c.FailedMessages.Any())
                .Run().ConfigureAwait(false);

            var exceptionMessage = context.FailedMessages.First().Value.First().Exception.Message;
            StringAssert.StartsWith("A TransactionScope has been opened in the current context overriding the one created by the transport.", exceptionMessage);
        }

        public class Context : ScenarioContext
        {
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<SqlServerTransport>().Transactions(TransportTransactionMode.TransactionScope);
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

            public class TestSaga12 : SqlSaga<TestSagaData12>, IAmStartedByMessages<StartSagaMessage>
            {
                protected override string CorrelationPropertyName => nameof(TestSagaData12.SomeId);

                public Context TestContext { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId);
                }
            }

            public class TestSagaData12 : ContainSagaData
            {
                public virtual Guid SomeId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }

            public bool SecondMessage { get; set; }
        }
    }
}