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

    public class When_canceling_previous_saga_timeouts : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_invoke_them()
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
                    var timeoutMessage = context.Message.Instance as SagaTimeout;
                    if (timeoutMessage != null)
                    {
                        if (timeoutMessage.Data == "1")
                        {
                            testContext.Timeout1Delivered = true;
                        }
                        else if (timeoutMessage.Data == "2")
                        {
                            testContext.Timeout2Delivered = true;
                        }
                    }
                }
            }

            public class CancellingTimeoutSaga : SqlSaga<CancellingTimeoutSaga.CancellingTimeoutSagaData>,
                IAmStartedByMessages<StartSaga>,
                IHandleMessages<SagaMessage>,
                IHandleTimeouts<SagaTimeout>
            {
                protected override string CorrelationPropertyName => nameof(CancellingTimeoutSagaData.DataId);

                public Context TestContext { get; set; }

                public async Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.DataId = message.DataId;
                    await RequestTimeout(context, TimeoutRequestBehavior.CancelPrevious, TimeSpan.FromSeconds(5), new SagaTimeout
                    {
                        Data = "1"
                    });
                    await context.SendLocal(new SagaMessage
                    {
                        DataId = message.DataId
                    });
                }

                public Task Timeout(SagaTimeout state, IMessageHandlerContext context)
                {
                    if (state.Data == "1")
                    {
                        TestContext.Timeout1Triggered = true;
                    }
                    else if (state.Data == "2")
                    {
                        TestContext.Timeout2Triggered = true;
                    }
                    return Task.FromResult(0);
                }

                public async Task Handle(SagaMessage message, IMessageHandlerContext context)
                {
                    await RequestTimeout(context, TimeoutRequestBehavior.CancelPrevious, TimeSpan.FromSeconds(5), new SagaTimeout
                    {
                        Data = "2"
                    });
                }

                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.DataId);
                    mapper.ConfigureMapping<SagaMessage>(m => m.DataId);
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

        public class SagaTimeout : IMessage
        {
            public string Data { get; set; }
        }

        public class SagaMessage : IMessage
        {
            public Guid DataId { get; set; }
        }
    }
}