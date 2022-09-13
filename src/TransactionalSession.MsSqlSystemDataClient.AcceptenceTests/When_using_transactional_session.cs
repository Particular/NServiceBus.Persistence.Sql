﻿namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using System.Data.SqlClient;
    using NUnit.Framework;
    using ObjectBuilder;
    using Persistence.Sql;
    using Persistence.Sql.ScriptBuilder;

    public class When_using_transactional_session : NServiceBusAcceptanceTest
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            MsSqlSystemDataClientConnectionBuilder.DropDbIfCollationIncorrect();
            MsSqlSystemDataClientConnectionBuilder.CreateDbIfNotExists();
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_messages_and_insert_rows_in_synchronized_session_on_transactional_session_commit(
            bool outboxEnabled)
        {
            if (outboxEnabled)
            {
                await CreateOutboxTable(Conventions.EndpointNamingConvention(typeof(AnEndpoint)));
            }

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

                    using (var insertCommand = new SqlCommand(insertText,
                               (SqlConnection)storageSession.Connection,
                               (SqlTransaction)storageSession.Transaction))
                    {
                        await insertCommand.ExecuteNonQueryAsync();
                    }

                    await transactionalSession.Commit().ConfigureAwait(false);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            using var connection = MsSqlSystemDataClientConnectionBuilder.Build();
            await connection.OpenAsync();

            using var queryCommand =
                new SqlCommand($"SELECT TOP 1 [Id] FROM [dbo].[SomeTable] WHERE [Id]='{rowId}'", connection);
            object result = await queryCommand.ExecuteScalarAsync();

            Assert.AreEqual(rowId, result);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_messages_and_insert_rows_in_sql_session_on_transactional_session_commit(
            bool outboxEnabled)
        {
            if (outboxEnabled)
            {
                await CreateOutboxTable(Conventions.EndpointNamingConvention(typeof(AnEndpoint)));
            }

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

                    string insertText =
                        $@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SomeTable' and xtype='U')
                                        BEGIN
	                                        CREATE TABLE [dbo].[SomeTable]([Id] [nvarchar](50) NOT NULL)
                                        END;
                                        INSERT INTO [dbo].[SomeTable] VALUES ('{rowId}')";

                    using (var insertCommand = new SqlCommand(insertText,
                               (SqlConnection)storageSession.Connection,
                               (SqlTransaction)storageSession.Transaction))
                    {
                        await insertCommand.ExecuteNonQueryAsync();
                    }

                    await transactionalSession.Commit().ConfigureAwait(false);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            using var connection = MsSqlSystemDataClientConnectionBuilder.Build();
            await connection.OpenAsync();

            using var queryCommand =
                new SqlCommand($"SELECT TOP 1 [Id] FROM [dbo].[SomeTable] WHERE [Id]='{rowId}'", connection);
            object result = await queryCommand.ExecuteScalarAsync();

            Assert.AreEqual(rowId, result);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_not_send_messages_if_session_is_not_committed(bool outboxEnabled)
        {
            if (outboxEnabled)
            {
                await CreateOutboxTable(Conventions.EndpointNamingConvention(typeof(AnEndpoint)));
            }

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

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_immediate_dispatch_messages_even_if_session_is_not_committed(bool outboxEnabled)
        {
            if (outboxEnabled)
            {
                await CreateOutboxTable(Conventions.EndpointNamingConvention(typeof(AnEndpoint)));
            }

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

        static async Task CreateOutboxTable(string endpointName)
        {
            string tablePrefix = endpointName.Replace('.', '_');
            using var connection = MsSqlSystemDataClientConnectionBuilder.Build();
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
            public AnEndpoint()
            {
                var useOutbox = (bool)TestContext.CurrentContext.Test.Arguments[0];
                if (useOutbox)
                {
                    EndpointSetup<TransactionSessionWithOutboxEndpoint>();
                }
                else
                {
                    EndpointSetup<TransactionSessionDefaultServer>();
                }
            }

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
