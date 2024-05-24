namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Persistence.Sql;
    using Pipeline;

    public class When_persisting_user_data_via_synchronized_session : NServiceBusAcceptanceTest
    {
        static string DataTableName => "MyPreciousTable";

        static string CreateUserDataTableText => $@"
create table if not exists ""public"".""{DataTableName}"" (
    Id uuid not null
);";

        [Test]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive, false)] //Uses shared DbConnection/DbTransaction to ensure exactly-once
        [TestCase(TransportTransactionMode.ReceiveOnly, true)] //Uses the Outbox to ensure exactly-once
        public async Task Should_rollback_changes_when_transport_transaction_is_rolled_back(TransportTransactionMode transactionMode, bool enableOutbox)
        {
            using (var connection = PostgreSqlConnectionBuilder.Build())
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = CreateUserDataTableText;
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(
                    b => b.When((session, ctx) => session.SendLocal(new MyMessage()
                    {
                        Id = ctx.TestRunId
                    })).DoNotFailOnErrorMessages().CustomConfig(c =>
                    {
                        c.ConfigureTransport().TransportTransactionMode = transactionMode;
                        if (enableOutbox)
                        {
                            c.EnableOutbox();
                        }
                    }))
                .Done(c => c.ReplyReceived)
                .Run();

            Assert.True(context.ReplyReceived);
            Assert.IsFalse(context.TransactionEscalatedToDTC);
            Assert.AreEqual(2, context.InvocationCount, "Handler should be called twice");
            Assert.AreEqual(1, context.RecordCount, "There should be only once record in the database");
        }

        public class Context : ScenarioContext
        {
            public int RecordCount { get; set; }
            public int InvocationCount { get; set; }
            public bool TransactionEscalatedToDTC { get; set; }
            public bool ReplyReceived { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.Pipeline.Register(new BehaviorThatThrowsAfterFirstMessage.Registration());
                    var recoverability = config.Recoverability();
                    recoverability.Immediate(settings =>
                    {
                        settings.NumberOfRetries(1);
                    });
                });
            }

            // Only needed to force SQL persistence to set up the synchronized storage session
            public class TestSaga : SqlSaga<TestSaga.SagaData>, IAmStartedByMessages<SagaMessage>
            {
                public Task Handle(SagaMessage message, IMessageHandlerContext context)
                {
                    throw new NotImplementedException();
                }

                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.ConfigureMapping<SagaMessage>(m => null);
                }

                protected override string CorrelationPropertyName => "TestRunId";
                public class SagaData : ContainSagaData
                {
                    public Guid TestRunId { get; set; }
                }
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                Context testContext;

                public MyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public async Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    if (message.Id != testContext.TestRunId)
                    {
                        return;
                    }

                    var session = context.SynchronizedStorageSession.SqlPersistenceSession();
                    var insertCommand = $@"insert into ""{DataTableName}"" (Id) VALUES (@Id)";
                    using (var command = session.Connection.CreateCommand())
                    {
                        command.Transaction = session.Transaction;
                        command.CommandText = insertCommand;
                        command.AddParameter("@Id", message.Id);
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }

                    int count;
                    var selectCommand = $@"select count(*) from ""{DataTableName}"" where Id = @Id";
                    using (var command = session.Connection.CreateCommand())
                    {
                        command.Transaction = session.Transaction;
                        command.CommandText = selectCommand;
                        command.AddParameter("@Id", message.Id);
                        count = (int)await command.ExecuteScalarAsync().ConfigureAwait(false);
                    }

                    testContext.RecordCount = count;
                    testContext.InvocationCount++;

                    await context.SendLocal(new ReplyMessage
                    {
                        Id = message.Id
                    });
                }
            }

            public class Handler : IHandleMessages<ReplyMessage>
            {
                Context testContext;

                public Handler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(ReplyMessage message, IMessageHandlerContext context)
                {
                    if (testContext.TestRunId == message.Id)
                    {
                        testContext.ReplyReceived = true;
                    }

                    return Task.CompletedTask;
                }
            }

            class BehaviorThatThrowsAfterFirstMessage : Behavior<IIncomingLogicalMessageContext>
            {
                Context testContext;

                public BehaviorThatThrowsAfterFirstMessage(Context context)
                {
                    testContext = context;
                }

                public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    await next();

                    if (testContext.InvocationCount == 1)
                    {
                        testContext.TransactionEscalatedToDTC = Transaction.Current.TransactionInformation.DistributedIdentifier != Guid.Empty;

                        throw new SimulatedException();
                    }
                }

                public class Registration : RegisterStep
                {
                    public Registration() : base("BehaviorThatThrowsAfterFirstMessage", typeof(BehaviorThatThrowsAfterFirstMessage), "BehaviorThatThrowsAfterFirstMessage")
                    {
                    }
                }
            }
        }

        public class SagaMessage : IMessage
        {
        }

        public class MyMessage : IMessage
        {
            public Guid Id { get; set; }
        }

        public class ReplyMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}