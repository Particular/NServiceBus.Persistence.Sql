namespace NServiceBus.AcceptanceTests;

using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;
using Persistence.Sql;
using IsolationLevel = System.Data.IsolationLevel;

[TestFixture]
public class When_outbox_in_ado_mode : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_work_with_snapshot_isolation()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage()))
                .CustomConfig(c => c.EnableOutbox().TransactionIsolationLevel(IsolationLevel.Snapshot)))
            .Done(c => c.Done)
            .Run();

        Assert.That(context.Transaction, Is.Not.Null, "Transaction should be available in handler");
        Assert.That(context.IsolationLevel, Is.EqualTo(IsolationLevel.Snapshot), "IsolationLevel should be honored");
    }

    public class Context : ScenarioContext
    {
        public bool Done { get; set; }
        public DbTransaction Transaction { get; set; }
        public IsolationLevel IsolationLevel { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<DefaultServer>(c => c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly);

        public class MyMessageHandler(Context context, ISqlStorageSession storageSession) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
            {
                context.Transaction = storageSession.Transaction;
                context.IsolationLevel = storageSession.Transaction.IsolationLevel;
                context.Done = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage
    {
    }
}