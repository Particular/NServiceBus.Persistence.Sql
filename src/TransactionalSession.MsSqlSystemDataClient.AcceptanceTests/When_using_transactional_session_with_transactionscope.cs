namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using System.Data.SqlClient;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    public class When_using_transactional_session_with_transactionscope : NServiceBusAcceptanceTest
    {
        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            MsSqlSystemDataClientConnectionBuilder.DropDbIfCollationIncorrect();
            MsSqlSystemDataClientConnectionBuilder.CreateDbIfNotExists();

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
                    using IServiceScope scope = ctx.ServiceProvider.CreateScope();
                    using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();
                    var sessionOptions = new SqlPersistenceOpenSessionOptions();
                    await transactionalSession.Open(sessionOptions);

                    await transactionalSession.SendLocal(new SampleMessage());

                    var storageSession = transactionalSession.SynchronizedStorageSession.SqlPersistenceSession();

                    string insertText = $@"INSERT INTO [dbo].[SomeTable] VALUES ('{rowId}')";

                    using (var insertCommand = new SqlCommand(insertText, (SqlConnection)storageSession.Connection))
                    {
                        await insertCommand.ExecuteNonQueryAsync();
                    }

                    var resultBeforeCommit = await QueryInsertedEntry(rowId);
                    Assert.That(resultBeforeCommit, Is.EqualTo(null));

                    await transactionalSession.Commit().ConfigureAwait(false);

                    // the transactional operations should be visible after commit
                    var resultBeforeAfterCommit = await QueryInsertedEntry(rowId);
                    Assert.That(resultBeforeAfterCommit, Is.EqualTo(rowId));
                }))
                .Done(c => c.MessageReceived)
                .Run();

            var resultAfterDispose = await QueryInsertedEntry(rowId);
            Assert.That(resultAfterDispose, Is.EqualTo(rowId));
        }

        static async Task<string> QueryInsertedEntry(string rowId)
        {
            using var __ = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
            using var connection = MsSqlSystemDataClientConnectionBuilder.Build();

            await connection.OpenAsync();

            using var queryCommand =
                new SqlCommand($"SET TRANSACTION ISOLATION LEVEL READ COMMITTED; SELECT TOP 1 [Id] FROM [dbo].[SomeTable] WITH (READPAST) WHERE [Id]='{rowId}' ",
                    connection);
            return (string)await queryCommand.ExecuteScalarAsync();
        }

        class Context : ScenarioContext, IInjectServiceProvider
        {
            public bool MessageReceived { get; set; }
            public bool CompleteMessageReceived { get; set; }
            public IServiceProvider ServiceProvider { get; set; }
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