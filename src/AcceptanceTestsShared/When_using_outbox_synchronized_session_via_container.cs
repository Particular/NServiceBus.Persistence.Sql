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
public class When_using_outbox_synchronized_session_via_container : NServiceBusAcceptanceTest
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

        Assert.True(context.RepositoryHasConnection);
    }

    public class Context : ScenarioContext
    {
        public bool Done { get; set; }
        public bool RepositoryHasConnection { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.EnableOutbox();
                c.RegisterComponents(cc =>
                {
                    cc.AddScoped<MyRepository>();
                    cc.AddScoped(b => b.GetRequiredService<ISqlStorageSession>().Connection);
                });
            });
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            Context context;
            MyRepository repository;

            public MyMessageHandler(MyRepository repository, Context context)
            {
                this.context = context;
                this.repository = repository;
            }


            public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
            {
                repository.DoSomething();
                context.Done = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyRepository
    {
        DbConnection connection;
        Context context;

        public MyRepository(DbConnection connection, Context context)
        {
            this.connection = connection;
            this.context = context;
        }

        public void DoSomething()
        {
            context.RepositoryHasConnection = connection != null;
        }
    }

    public class MyMessage : IMessage
    {
        public string Property { get; set; }
    }

}