using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;

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

        Assert.That(context.ConnectionInjectedToFirstHandler, Is.Not.Null);
        Assert.That(context.ConnectionInjectedToSecondHandler, Is.Not.Null);
        Assert.That(context.ConnectionInjectedToThirdHandler, Is.Not.Null);
        Assert.That(context.ConnectionInjectedToSecondHandler, Is.SameAs(context.ConnectionInjectedToFirstHandler));
        Assert.AreNotSame(context.ConnectionInjectedToFirstHandler, context.ConnectionInjectedToThirdHandler);
    }

    public class Context : ScenarioContext
    {
        public bool Done { get; set; }
        public DbConnection ConnectionInjectedToFirstHandler { get; set; }
        public DbConnection ConnectionInjectedToSecondHandler { get; set; }
        public DbConnection ConnectionInjectedToThirdHandler { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint()
        {
            EndpointSetup<DefaultServer>(config =>
            {
                config.RegisterComponents(c =>
                {
                    c.AddScoped(b =>
                    {
                        var session = b.GetRequiredService<ISqlStorageSession>();
                        return new DataContext(session.Connection);
                    });
                });
            });
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            Context context;
            DataContext dataContext;

            public MyMessageHandler(DataContext storageSession, Context context)
            {
                this.dataContext = storageSession;
                this.context = context;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
            {
                context.ConnectionInjectedToFirstHandler = dataContext.Connection;
                return handlerContext.SendLocal(new MyFollowUpMessage
                {
                    Property = message.Property
                });
            }
        }

        public class MyOtherMessageHandler : IHandleMessages<MyMessage>
        {
            Context context;
            DataContext dataContext;

            public MyOtherMessageHandler(DataContext dataContext, Context context)
            {
                this.dataContext = dataContext;
                this.context = context;
            }


            public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
            {
                context.ConnectionInjectedToSecondHandler = dataContext.Connection;
                return Task.CompletedTask;
            }
        }

        public class MyFollowUpMessageHandler : IHandleMessages<MyFollowUpMessage>
        {
            Context context;
            DataContext dataContext;

            public MyFollowUpMessageHandler(DataContext dataContext, Context context)
            {
                this.dataContext = dataContext;
                this.context = context;
            }


            public Task Handle(MyFollowUpMessage message, IMessageHandlerContext handlerContext)
            {
                context.ConnectionInjectedToThirdHandler = dataContext.Connection;
                context.Done = true;
                return Task.CompletedTask;
            }
        }
    }

    public class DataContext
    {
        public DataContext(DbConnection connection)
        {
            Connection = connection;
        }

        public DbConnection Connection { get; }
    }

    public class MyMessage : IMessage
    {
        public string Property { get; set; }
    }

    public class MyFollowUpMessage : IMessage
    {
        public string Property { get; set; }
    }

}