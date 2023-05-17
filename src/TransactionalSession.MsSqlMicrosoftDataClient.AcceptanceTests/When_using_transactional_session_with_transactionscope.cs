namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using Microsoft.Data.SqlClient;
    using NUnit.Framework;
    using ObjectBuilder;

    public class When_using_transactional_session_with_transactionscope : NServiceBusAcceptanceTest
    {
        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            MsSqlMicrosoftDataClientConnectionBuilder.DropDbIfCollationIncorrect();
            MsSqlMicrosoftDataClientConnectionBuilder.CreateDbIfNotExists();

            await OutboxHelpers.CreateDataTable();
        }

        [Test]
        public async Task Should_provide_ambient_transactionscope()
        {
            await OutboxHelpers.CreateOutboxTable<AnEndpoint>();

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

                    string insertText =
                        $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SomeTable' and xtype='U')
                                        BEGIN
	                                        CREATE TABLE [dbo].[SomeTable]([Id] [nvarchar](50) NOT NULL)
                                        END;
                                        INSERT INTO [dbo].[SomeTable] VALUES ('{rowId}')";

                    using (var insertCommand = new SqlCommand(insertText, (SqlConnection)storageSession.Connection))
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
            using var __ = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
            using var connection = MsSqlMicrosoftDataClientConnectionBuilder.Build();

            await connection.OpenAsync();

            using var queryCommand =
                new SqlCommand($"SET TRANSACTION ISOLATION LEVEL READ COMMITTED; SELECT TOP 1 [Id] FROM [dbo].[SomeTable] WITH (READPAST) WHERE [Id]='{rowId}' ",
                    connection);
            return (string)await queryCommand.ExecuteScalarAsync();
        }

        class Context : ScenarioContext, IInjectBuilder
        {
            public bool MessageReceived { get; set; }
            public bool CompleteMessageReceived { get; set; }
            public IBuilder Builder { get; set; }
        }

        class AnEndpoint : EndpointConfigurationBuilder
        {
            public AnEndpoint() => EndpointSetup<TransactionSessionWithOutboxEndpoint>(c => c.EnableOutbox().UseTransactionScope());

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