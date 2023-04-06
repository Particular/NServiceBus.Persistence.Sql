namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Microsoft.Data.SqlClient;
    using NUnit.Framework;
    using ObjectBuilder;
    using Persistence.Sql.ScriptBuilder;

    public class When_using_transactional_session_with_transactionscope : NServiceBusAcceptanceTest
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            MsSqlMicrosoftDataClientConnectionBuilder.DropDbIfCollationIncorrect();
            MsSqlMicrosoftDataClientConnectionBuilder.CreateDbIfNotExists();
        }

        [Test]
        public async Task Should_provide_ambient_transactionscope()
        {
            await CreateOutboxTable(Conventions.EndpointNamingConvention(typeof(AnEndpoint)));

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

                    using (var __ = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        using var connection = MsSqlMicrosoftDataClientConnectionBuilder.Build();

                        await connection.OpenAsync();

                        using var queryCommand =
                            new SqlCommand($"SELECT TOP 1 [Id] FROM [dbo].[SomeTable] WITH (READPAST) WHERE [Id]='{rowId}' ", connection);
                        object result = await queryCommand.ExecuteScalarAsync();

                        Assert.AreEqual(null, result);
                    }

                    await transactionalSession.Commit().ConfigureAwait(false);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            using var connection = MsSqlMicrosoftDataClientConnectionBuilder.Build();
            await connection.OpenAsync();

            using var queryCommand =
                new SqlCommand($"SELECT TOP 1 [Id] FROM [dbo].[SomeTable] WHERE [Id]='{rowId}'", connection);
            object result = await queryCommand.ExecuteScalarAsync();

            Assert.AreEqual(rowId, result);
        }


        static async Task CreateOutboxTable(string endpointName)
        {
            string tablePrefix = endpointName.Replace('.', '_');
            using var connection = MsSqlMicrosoftDataClientConnectionBuilder.Build();
            await connection.OpenAsync().ConfigureAwait(false);

            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(BuildSqlDialect.MsSqlServer), tablePrefix);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(BuildSqlDialect.MsSqlServer), tablePrefix);
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