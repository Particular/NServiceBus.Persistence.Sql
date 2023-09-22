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
    [TestCase(TransportTransactionMode.TransactionScope)] //Uses TransactionScope to ensure exactly-once
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)] //Uses shared DbConnection/DbTransaction to ensure exactly-once
    public async Task Should_inject_synchronized_session_into_handler(TransportTransactionMode transactionMode)
    {
        // The EndpointsStarted flag is set by acceptance framework
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage())).CustomConfig(cfg =>
            {
                cfg.ConfigureTransport().TransportTransactionMode = transactionMode;
            }))
            .Done(c => c.Done)
            .Run()
            .ConfigureAwait(false);

        Assert.NotNull(context.InjectedSession.Connection);
        if (transactionMode == TransportTransactionMode.TransactionScope)
        {
            Assert.IsNull(context.InjectedSession.Transaction);
        }
        else
        {
            Assert.IsNotNull(context.InjectedSession.Transaction);
        }
    }

    public class Context : ScenarioContext
    {
        public bool Done { get; set; }
        public ISqlStorageSession InjectedSession { get; set; }
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
                context.InjectedSession = storageSession;
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage
    {
        public string Property { get; set; }
    }

}