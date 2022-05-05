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
    [Test]
    public async Task Should_float_transaction_scope_into_handler()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage())))
            .Done(c => c.Done)
            .Run()
            .ConfigureAwait(false);

        Assert.NotNull(context.Transaction);
    }

    public class Context : ScenarioContext
    {
        public bool Done { get; set; }
        public Transaction Transaction { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                c.EnableOutbox().UseTransactionScope();
            });
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            Context context;

            public MyMessageHandler(Context context)
            {
                this.context = context;
            }


            public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
            {
                context.Transaction = Transaction.Current;
                context.Done = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage
    {
        public string Property { get; set; }
    }

}