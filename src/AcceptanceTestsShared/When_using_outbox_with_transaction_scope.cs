using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

[TestFixture]
public class When_using_outbox_with_transaction_scope : NServiceBusAcceptanceTest
{
    [TestCase(IsolationLevel.ReadCommitted)]
    [TestCase(IsolationLevel.Snapshot)]
    public async Task Should_float_transaction_scope_into_handler(IsolationLevel isolationLevel)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage()))
                .CustomConfig(c => c.EnableOutbox().UseTransactionScope(isolationLevel)))
            .Done(c => c.Done)
            .Run();

        Assert.That(context.Transaction, Is.Not.Null, "Ambient transaction should be available in handler");
        Assert.That(context.IsolationLevel, Is.EqualTo(isolationLevel), "IsolationLevel should be honored");
    }

    public class Context : ScenarioContext
    {
        public bool Done { get; set; }
        public Transaction Transaction { get; set; }
        public IsolationLevel IsolationLevel { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<DefaultServer>(c => c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly);

        public class MyMessageHandler(Context context) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
            {
                context.Transaction = Transaction.Current;
                if (Transaction.Current != null)
                {
                    context.IsolationLevel = Transaction.Current.IsolationLevel;
                }

                context.Done = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage
    {
    }
}