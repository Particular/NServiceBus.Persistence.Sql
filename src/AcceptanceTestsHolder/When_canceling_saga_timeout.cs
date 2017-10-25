namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Persistence.Sql;

    public class When_canceling_saga_timeout : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_invoke_it()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new StartSaga
                {
                    DataId = Guid.NewGuid()
                })))
                .Done(c => c.Timeout1Delivered && c.Timeout2Delivered)
                .Run();

            Assert.True(context.Timeout1Delivered);
            Assert.True(context.Timeout2Delivered);

            Assert.False(context.Timeout1Triggered);
            Assert.True(context.Timeout2Triggered);
        }

        public class Context : ScenarioContext
        {
            public bool Timeout1Triggered { get; set; }
            public bool Timeout2Triggered { get; set; }
            public bool Timeout1Delivered { get; set; }
            public bool Timeout2Delivered { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.Pipeline.Register(b => new TimeoutSpyBehavior(b.Build<Context>()), "TimeoutSpyBehavior");
                    config.LimitMessageProcessingConcurrencyTo(1);
                });
            }

            public class TimeoutSpyBehavior : Behavior<IIncomingLogicalMessageContext>
            {
                Context testContext;

                public TimeoutSpyBehavior(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    await next();
                    var timeout1Message = context.Message.Instance as SagaTimeout1;
                    if (timeout1Message != null)
                    {
                        testContext.Timeout1Delivered = true;
                    }
                    var timeout2Message = context.Message.Instance as SagaTimeout2;
                    if (timeout2Message != null)
                    {
                        testContext.Timeout2Delivered = true;
                    }
                }
            }

            public class CancellingTimeoutSaga : SqlSaga<CancellingTimeoutSaga.CancellingTimeoutSagaData>,
                IAmStartedByMessages<StartSaga>,
                IHandleTimeouts<SagaTimeout1>,
                IHandleTimeouts<SagaTimeout2>
            {
                protected override string CorrelationPropertyName => nameof(CancellingTimeoutSagaData.DataId);

                public Context TestContext { get; set; }

                public async Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.DataId = message.DataId;
                    var timeoutId = Guid.NewGuid();
                    await RequestTimeout(context, timeoutId, TimeSpan.FromSeconds(2), new SagaTimeout1());
                    await RequestTimeout(context, TimeSpan.FromSeconds(4), new SagaTimeout2());
                    CancelTimeout(context, timeoutId);
                }

                public Task Timeout(SagaTimeout1 state, IMessageHandlerContext context)
                {
                    TestContext.Timeout1Triggered = true;
                    return Task.FromResult(0);
                }

                public Task Timeout(SagaTimeout2 state, IMessageHandlerContext context)
                {
                    TestContext.Timeout2Triggered = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.DataId);
                }

                public class CancellingTimeoutSagaData : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }
            }
        }

        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }

        public class SagaTimeout1 : IMessage
        {
        }

        public class SagaTimeout2 : IMessage
        {
        }
    }
}