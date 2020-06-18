using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;

[TestFixture]
public class When_using_synchronized_session_via_container : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_inject_synchronized_session_into_handler()
    {
        // The EndpointsStarted flag is set by acceptance framework
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage())))
            .Done(c => c.Done)
            .Run()
            .ConfigureAwait(false);

        Assert.True(context.SessionInjected);
    }

    public class Context : ScenarioContext
    {
        public bool Done { get; set; }
        public bool SessionInjected { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint()
        {
            EndpointSetup<DefaultServer>(c =>
            {
            });
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            Context context;
            ISqlStorageSession storageSession;

            public MyMessageHandler(ISqlStorageSession storageSession, Context context)
            {
                this.storageSession = storageSession;
                this.context = context;
            }


            public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
            {
                context.Done = true;
                context.SessionInjected = storageSession != null;
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage
    {
        public string Property { get; set; }
    }

}