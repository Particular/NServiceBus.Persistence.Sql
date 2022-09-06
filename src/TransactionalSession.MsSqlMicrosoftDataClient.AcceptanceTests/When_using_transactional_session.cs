namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Persistence.Sql;
    using TransactionalSession;

    public class When_using_transactional_session : NServiceBusAcceptanceTest
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_messages_and_insert_rows_in_synchronized_session_on_transactional_session_commit(
            bool outboxEnabled)
        {
            string rowId = Guid.NewGuid().ToString();

            await Scenario.Define<Context>()
                          .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                          {
                              using IServiceScope scope = ctx.ServiceProvider.CreateScope();
                              using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();
                              var sessionOptions = new SqlPersistenceOpenSessionOptions();
                              await transactionalSession.Open(sessionOptions);

                              await transactionalSession.SendLocal(new SampleMessage(), CancellationToken.None);

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

                              await transactionalSession.Commit(CancellationToken.None).ConfigureAwait(false);
                          }))
                          .Done(c => c.MessageReceived)
                          .Run();

            using var connection = MsSqlMicrosoftDataClientConnectionBuilder.BuildWithoutCertificateCheck();
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
            string rowId = Guid.NewGuid().ToString();

            await Scenario.Define<Context>()
                          .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                          {
                              using IServiceScope scope = ctx.ServiceProvider.CreateScope();
                              using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();
                              var sessionOptions = new SqlPersistenceOpenSessionOptions();
                              await transactionalSession.Open(sessionOptions);

                              await transactionalSession.SendLocal(new SampleMessage(), CancellationToken.None);

                              ISqlStorageSession storageSession = scope.ServiceProvider.GetRequiredService<ISqlStorageSession>();

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

                              await transactionalSession.Commit(CancellationToken.None).ConfigureAwait(false);
                          }))
                          .Done(c => c.MessageReceived)
                          .Run();

            using var connection = MsSqlMicrosoftDataClientConnectionBuilder.BuildWithoutCertificateCheck();
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
            Context result = await Scenario.Define<Context>()
                                           .WithEndpoint<AnEndpoint>(s => s.When(async (statelessSession, ctx) =>
                                           {
                                               using (IServiceScope scope = ctx.ServiceProvider.CreateScope())
                                               using (var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>())
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
            Context result = await Scenario.Define<Context>()
                                           .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                                           {
                                               using IServiceScope scope = ctx.ServiceProvider.CreateScope();
                                               using var transactionalSession = scope.ServiceProvider
                                                   .GetRequiredService<ITransactionalSession>();

                                               var sessionOptions = new SqlPersistenceOpenSessionOptions();
                                               await transactionalSession.Open(sessionOptions);

                                               var sendOptions = new SendOptions();
                                               sendOptions.RequireImmediateDispatch();
                                               sendOptions.RouteToThisEndpoint();
                                               await transactionalSession.Send(new SampleMessage(), sendOptions,
                                                   CancellationToken.None);
                                           }))
                                           .Done(c => c.MessageReceived)
                                           .Run()
                ;

            Assert.True(result.MessageReceived);
        }

        class Context : ScenarioContext, IInjectServiceProvider
        {
            public bool MessageReceived { get; set; }
            public bool CompleteMessageReceived { get; set; }
            public IServiceProvider ServiceProvider { get; set; }
        }

        class AnEndpoint : EndpointConfigurationBuilder
        {
            public AnEndpoint()
            {
                if ((bool)TestContext.CurrentContext.Test.Arguments[0]!)
                {
                    EndpointSetup<DefaultServer>((endpointConfiguration, descriptor) =>
                    {
                        endpointConfiguration.RegisterStartupTask(provider =>
                            new CaptureServiceProviderStartupTask(provider, descriptor.ScenarioContext));
                        endpointConfiguration.TypesToIncludeInScan(new[] { typeof(SqlPersistenceTransactionalSession), typeof(TransactionalSession) });
                    });
                }
                else
                {
                    EndpointSetup<DefaultServer>((endpointConfiguration, runDescriptor) =>
                    {
                        endpointConfiguration.EnableOutbox();
                        endpointConfiguration.RegisterStartupTask(provider =>
                            new CaptureServiceProviderStartupTask(provider, runDescriptor.ScenarioContext));
                        endpointConfiguration.TypesToIncludeInScan(new[] { typeof(SqlPersistenceTransactionalSession), typeof(TransactionalSession) });
                    });
                }
            }

            class SampleHandler : IHandleMessages<SampleMessage>
            {
                readonly Context testContext;
                public SampleHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(SampleMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;

                    return Task.CompletedTask;
                }
            }

            class CompleteTestMessageHandler : IHandleMessages<CompleteTestMessage>
            {
                readonly Context testContext;
                public CompleteTestMessageHandler(Context context) => testContext = context;

                public Task Handle(CompleteTestMessage message, IMessageHandlerContext context)
                {
                    testContext.CompleteMessageReceived = true;

                    return Task.CompletedTask;
                }
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
