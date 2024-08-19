using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Features;
using NUnit.Framework;

[TestFixture]
//The SQL persistence is still configured for both outbox and sagas
public class When_sagas_and_outbox_are_disabled : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_create_synchronized_storage_session()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithSagasDisabled>(bb => bb.When(s => s.SendLocal(new MyMessage())))
            .Done(c => c.Done)
            .Run()
            .ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(context.Done, Is.True);
            Assert.That(context.SessionCreated, Is.True);
        });
    }

    public class Context : ScenarioContext
    {
        public bool SessionCreated { get; set; }
        public bool Done { get; set; }
    }

    public class EndpointWithSagasDisabled : EndpointConfigurationBuilder
    {
        public EndpointWithSagasDisabled()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.DisableFeature<Sagas>();
            });
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            Context testContext;

            public MyMessageHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                var session = context.SynchronizedStorageSession.SqlPersistenceSession();
                testContext.SessionCreated = session != null;
                testContext.Done = true;

                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage
    {
    }
}