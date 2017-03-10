namespace NServiceBus.AcceptanceTests.Routing.AutomaticSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Persistence.Sql;

    [TestFixture]
    public class When_starting_an_endpoint_with_a_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_autoSubscribe_the_saga_messageHandler_by_default()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Subscriber>()
                .Done(c => c.EventsSubscribedTo.Count >= 2)
                .Run();

            Assert.True(context.EventsSubscribedTo.Contains(typeof(MyEvent)), "Events only handled by sagas should be auto subscribed");
            Assert.True(context.EventsSubscribedTo.Contains(typeof(MyEventBase)), "Sagas should be auto subscribed even when handling a base class event");
        }

        class Context : ScenarioContext
        {
            public Context()
            {
                EventsSubscribedTo = new List<Type>();
            }

            public List<Type> EventsSubscribedTo { get; }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register("SubscriptionSpy", new SubscriptionSpy((Context) ScenarioContext), "Spies on subscriptions made"),
                    metadata =>
                    {
                        metadata.RegisterPublisherFor<MyEventBase>(typeof(Subscriber));
                        metadata.RegisterPublisherFor<MyEvent>(typeof(Subscriber));
                    });
            }

            class SubscriptionSpy : IBehavior<ISubscribeContext, ISubscribeContext>
            {
                public SubscriptionSpy(Context testContext)
                {
                    this.testContext = testContext;
                }

                public async Task Invoke(ISubscribeContext context, Func<ISubscribeContext, Task> next)
                {
                    await next(context).ConfigureAwait(false);

                    testContext.EventsSubscribedTo.Add(context.EventType);
                }

                Context testContext;
            }

            [SqlSaga(correlationProperty: nameof(AutoSubscriptionSagaData.SomeId))]
            public class AutoSubscriptionSaga : SqlSaga<AutoSubscriptionSaga.AutoSubscriptionSagaData>, IAmStartedByMessages<MyEvent>
            {
                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.MapMessage<MyEvent>(msg => msg.SomeId);
                }

                public class AutoSubscriptionSagaData : ContainSagaData
                {
                    public virtual string SomeId { get; set; }
                }
            }

            [SqlSaga(correlationProperty: nameof(SagaData.SomeId))]
            public class SagaThatReactsToSuperEvent : SqlSaga<SagaThatReactsToSuperEvent.SagaData>,
                IAmStartedByMessages<MyEventBase>
            {
                public Task Handle(MyEventBase message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.MapMessage<MyEventBase>(msg => msg.SomeId);
                }

                public class SagaData : ContainSagaData
                {
                    public virtual string SomeId { get; set; }
                }
            }
        }

        public class MyEventBase : IEvent
        {
            public string SomeId { get; set; }
        }

        public class MyEventWithParent : MyEventBase
        {
        }

        public class MyEvent : IEvent
        {
            public string SomeId { get; set; }
        }
    }
}