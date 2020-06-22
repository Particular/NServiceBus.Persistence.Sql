using System.Data.Common;
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
            .Done(c => c.ConnectionInjectedToFirstHandler != null && c.ConnectionInjectedToSecondHandler != null)
            .Run()
            .ConfigureAwait(false);

        Assert.IsNotNull(context.ConnectionInjectedToFirstHandler);
        Assert.IsNotNull(context.ConnectionInjectedToSecondHandler);
        Assert.AreSame(context.ConnectionInjectedToFirstHandler, context.ConnectionInjectedToSecondHandler);
    }

    public class Context : ScenarioContext
    {
        public bool Done { get; set; }
        public DbConnection ConnectionInjectedToFirstHandler { get; set; }
        public DbConnection ConnectionInjectedToSecondHandler { get; set; }
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
                context.ConnectionInjectedToFirstHandler = storageSession.Connection;
                return Task.CompletedTask;
            }
        }

        public class MyOtherMessageHandler : IHandleMessages<MyMessage>
        {
            Context context;
            ISqlStorageSession storageSession;

            public MyOtherMessageHandler(ISqlStorageSession storageSession, Context context)
            {
                this.storageSession = storageSession;
                this.context = context;
            }


            public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
            {
                context.Done = true;
                context.ConnectionInjectedToSecondHandler = storageSession.Connection;
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage
    {
        public string Property { get; set; }
    }

}