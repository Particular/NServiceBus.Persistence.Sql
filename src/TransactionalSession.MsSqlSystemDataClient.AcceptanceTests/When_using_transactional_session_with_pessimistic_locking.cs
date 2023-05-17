namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Data.SqlClient;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ObjectBuilder;
    using Persistence.Sql;

    public class When_using_transactional_session_with_pessimistic_locking : NServiceBusAcceptanceTest
    {
        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            MsSqlSystemDataClientConnectionBuilder.DropDbIfCollationIncorrect();
            MsSqlSystemDataClientConnectionBuilder.CreateDbIfNotExists();

            await OutboxHelpers.CreateDataTable();
        }

        [SetUp]
        public async Task Setup() =>
            await OutboxHelpers.CreateOutboxTable<AnEndpoint>();

        [Test]
        public async Task Should_send_messages_and_insert_rows_in_synchronized_session_on_transactional_session_commit()
        {
            string rowId = Guid.NewGuid().ToString();

            await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.Builder.CreateChildBuilder();
                    using var transactionalSession = scope.Build<ITransactionalSession>();
                    var sessionOptions = new SqlPersistenceOpenSessionOptions();
                    await transactionalSession.Open(sessionOptions);

                    await transactionalSession.SendLocal(new SampleMessage());

                    var storageSession = transactionalSession.SynchronizedStorageSession.SqlPersistenceSession();

                    string insertText = $@"INSERT INTO [dbo].[SomeTable] VALUES ('{rowId}')";

                    using (var insertCommand = new SqlCommand(insertText,
                               (SqlConnection)storageSession.Connection,
                               (SqlTransaction)storageSession.Transaction))
                    {
                        await insertCommand.ExecuteNonQueryAsync();
                    }

                    // the transactional operations should not be visible before commit
                    var resultBeforeCommit = await QueryInsertedEntry(rowId);
                    Assert.AreEqual(null, resultBeforeCommit);

                    await transactionalSession.Commit().ConfigureAwait(false);

                    // the transactional operations should be visible after commit
                    var resultBeforeAfterCommit = await QueryInsertedEntry(rowId);
                    Assert.AreEqual(rowId, resultBeforeAfterCommit);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            var resultAfterDispose = await QueryInsertedEntry(rowId);
            Assert.AreEqual(rowId, resultAfterDispose);
        }

        [Test]
        public async Task Should_send_messages_and_insert_rows_in_sql_session_on_transactional_session_commit()
        {
            string rowId = Guid.NewGuid().ToString();

            await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.Builder.CreateChildBuilder();
                    using var transactionalSession = scope.Build<ITransactionalSession>();
                    var sessionOptions = new SqlPersistenceOpenSessionOptions();
                    await transactionalSession.Open(sessionOptions);

                    await transactionalSession.SendLocal(new SampleMessage());

                    ISqlStorageSession storageSession = scope.Build<ISqlStorageSession>();

                    string insertText = $@"INSERT INTO [dbo].[SomeTable] VALUES ('{rowId}')";

                    using (var insertCommand = new SqlCommand(insertText,
                               (SqlConnection)storageSession.Connection,
                               (SqlTransaction)storageSession.Transaction))
                    {
                        await insertCommand.ExecuteNonQueryAsync();
                    }

                    // the transactional operations should not be visible before commit
                    var resultBeforeCommit = await QueryInsertedEntry(rowId);
                    Assert.AreEqual(null, resultBeforeCommit);

                    await transactionalSession.Commit().ConfigureAwait(false);

                    // the transactional operations should be visible after commit
                    var resultBeforeAfterCommit = await QueryInsertedEntry(rowId);
                    Assert.AreEqual(rowId, resultBeforeAfterCommit);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            var resultAfterDispose = await QueryInsertedEntry(rowId);
            Assert.AreEqual(rowId, resultAfterDispose);
        }

        static async Task<string> QueryInsertedEntry(string rowId)
        {
            using var connection = MsSqlSystemDataClientConnectionBuilder.Build();

            await connection.OpenAsync();

            using var queryCommand =
                new SqlCommand($"SET TRANSACTION ISOLATION LEVEL READ COMMITTED; SELECT TOP 1 [Id] FROM [dbo].[SomeTable] WITH (READPAST) WHERE [Id]='{rowId}' ",
                    connection);
            return (string)await queryCommand.ExecuteScalarAsync();
        }

        [Test]
        public async Task Should_not_send_messages_if_session_is_not_committed()
        {
            var result = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (statelessSession, ctx) =>
                {
                    using (var scope = ctx.Builder.CreateChildBuilder())
                    using (var transactionalSession = scope.Build<ITransactionalSession>())
                    {
                        var sessionOptions = new SqlPersistenceOpenSessionOptions();
                        await transactionalSession.Open(sessionOptions);

                        await transactionalSession.SendLocal(new SampleMessage());
                    }

                    //Send immediately dispatched message to finish the test
                    await statelessSession.SendLocal(new CompleteTestMessage());
                }))
                .Done(c => c.CompleteMessageReceived)
                .Run();

            Assert.True(result.CompleteMessageReceived);
            Assert.False(result.MessageReceived);
        }

        [Test]
        public async Task Should_send_immediate_dispatch_messages_even_if_session_is_not_committed()
        {
            var result = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.Builder.CreateChildBuilder();
                    using var transactionalSession = scope.Build<ITransactionalSession>();

                    var sessionOptions = new SqlPersistenceOpenSessionOptions();
                    await transactionalSession.Open(sessionOptions);

                    var sendOptions = new SendOptions();
                    sendOptions.RequireImmediateDispatch();
                    sendOptions.RouteToThisEndpoint();
                    await transactionalSession.Send(new SampleMessage(), sendOptions);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.True(result.MessageReceived);
        }

        class Context : ScenarioContext, IInjectBuilder
        {
            public bool MessageReceived { get; set; }
            public bool CompleteMessageReceived { get; set; }
            public IBuilder Builder { get; set; }
        }

        class AnEndpoint : EndpointConfigurationBuilder
        {
            public AnEndpoint() => EndpointSetup<TransactionSessionWithOutboxEndpoint>(c => c.EnableOutbox().UsePessimisticConcurrencyControl());

            class SampleHandler : IHandleMessages<SampleMessage>
            {
                public SampleHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(SampleMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;

                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }

            class CompleteTestMessageHandler : IHandleMessages<CompleteTestMessage>
            {
                public CompleteTestMessageHandler(Context context) => testContext = context;

                public Task Handle(CompleteTestMessage message, IMessageHandlerContext context)
                {
                    testContext.CompleteMessageReceived = true;

                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }
        }

        class SampleMessage : ICommand
        {
        }

        class CompleteTestMessage : ICommand
        {
        }
    }
}