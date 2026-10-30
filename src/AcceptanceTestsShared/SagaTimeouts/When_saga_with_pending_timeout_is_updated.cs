using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

[TestFixture]
public class When_saga_with_pending_timeout_is_updated : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_keep_the_timeout_and_fire_it_with_the_stored_payload()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(e => e.When(s => s.SendLocal(new StartSaga { Id = Guid.NewGuid().ToString() })))
            .Done(c => c.TimeoutFired)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.TimeoutsFoundAfterUpdate, Is.EqualTo(1));
            Assert.That(context.TimeoutValue, Is.EqualTo("Original"));
            Assert.That(context.UpdatesSeenByTimeout, Is.EqualTo(2));
        }
    }

    public class Context : ScenarioContext
    {
        public int TimeoutsFoundAfterUpdate { get; set; }
        public string TimeoutValue { get; set; }
        public int UpdatesSeenByTimeout { get; set; }
        public bool TimeoutFired { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        public class PendingTimeoutSaga(Context testContext) : Saga<PendingTimeoutSaga.SagaData>,
            IAmStartedByMessages<StartSaga>,
            IHandleMessages<UpdateSaga>,
            IHandleTimeouts<MyTimeout>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
                mapper.MapSaga(s => s.CorrelationProperty)
                    .ToMessage<StartSaga>(m => m.Id)
                    .ToMessage<UpdateSaga>(m => m.Id);

            public async Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                await RequestTimeout(context, TimeSpan.FromSeconds(3), new MyTimeout { Value = "Original" });
                await context.SendLocal(new UpdateSaga { Id = message.Id, Remaining = 2 });
            }

            // Updates that don't touch timeouts must keep the stored timeouts in the saga's metadata
            public async Task Handle(UpdateSaga message, IMessageHandlerContext context)
            {
                Data.Updates++;
                testContext.TimeoutsFoundAfterUpdate = (await FindTimeouts<MyTimeout>(_ => true)).Count;

                if (message.Remaining > 1)
                {
                    await context.SendLocal(new UpdateSaga { Id = message.Id, Remaining = message.Remaining - 1 });
                }
            }

            public Task Timeout(MyTimeout state, IMessageHandlerContext context)
            {
                testContext.TimeoutValue = state.Value;
                testContext.UpdatesSeenByTimeout = Data.Updates;
                testContext.TimeoutFired = true;
                MarkAsComplete();
                return Task.CompletedTask;
            }

            public class SagaData : ContainSagaData
            {
                public virtual string CorrelationProperty { get; set; }
                public virtual int Updates { get; set; }
            }
        }
    }

    public class StartSaga : ICommand
    {
        public string Id { get; set; }
    }

    public class UpdateSaga : ICommand
    {
        public string Id { get; set; }
        public int Remaining { get; set; }
    }

    public class MyTimeout
    {
        public string Value { get; set; }
    }
}
