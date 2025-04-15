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
            .Run();

        Assert.That(context.MessageHandlerSpy.ConnectionWasNotNullWhenHandleCalled);
        if (transactionMode == TransportTransactionMode.TransactionScope)
        {
            Assert.That(context.MessageHandlerSpy.TransactionWasNullWhenHandleCalled);
        }
        else
        {
            Assert.That(context.MessageHandlerSpy.TransactionWasNotNullWhenHandleCalled);
        }
    }

    public class Context : ScenarioContext
    {
        public bool Done { get; set; }
        public ISqlStorageSession InjectedSession { get; set; }

        public StorageSessionMessageHandlerSpy MessageHandlerSpy { get; set; }
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

                context.MessageHandlerSpy.ConnectionWasNotNullWhenHandleCalled =
                    context.InjectedSession.Connection != null;

                context.MessageHandlerSpy.TransactionWasNotNullWhenHandleCalled =
                    context.InjectedSession.Transaction != null;

                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage
    {
        public string Property { get; set; }
    }

    public class StorageSessionMessageHandlerSpy
    {
        //InjectedSession.Transaction is disposed and set to null after the message hanlder's
        //Handle method executes, so it is captured when Handle is called
        public bool TransactionWasNotNullWhenHandleCalled { get; set; }

        public bool TransactionWasNullWhenHandleCalled => !TransactionWasNotNullWhenHandleCalled;

        //InjectedSession.Connection is disposed and set to null after the message hanlder's
        //Handle method executes, so it is captured when Handle is called
        public bool ConnectionWasNotNullWhenHandleCalled { get; set; }

        public bool ConnectionWasNullWhenHandleCalled => !ConnectionWasNotNullWhenHandleCalled;
    }

}