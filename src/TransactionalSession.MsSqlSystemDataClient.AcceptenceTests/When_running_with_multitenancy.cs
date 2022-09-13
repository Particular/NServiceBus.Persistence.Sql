namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Persistence.Sql.ScriptBuilder;

    public class When_running_with_multitenancy : NServiceBusAcceptanceTest
    {
        static readonly string tenantId = "aTenant";
        static readonly string tenantIdHeaderName = "TenantName";

        [OneTimeSetUp]
        public void SetUpTenantDatabases() => MsSqlSystemDataClientConnectionBuilder.MultiTenant.Setup(tenantId);

        [OneTimeTearDown]
        public void TearDownTenantDatabases() => MsSqlSystemDataClientConnectionBuilder.MultiTenant.TearDown(tenantId);

        [Test]
        public async Task Should_send_messages_on_transactional_session_commit()
        {
            await CreateOutboxTable(tenantId, Conventions.EndpointNamingConvention(typeof(AnEndpoint)));

            await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using IServiceScope scope = ctx.ServiceProvider.CreateScope();
                    using ITransactionalSession transactionalSession =
                        scope.ServiceProvider.GetRequiredService<ITransactionalSession>();

                    var sessionOptions = new SqlPersistenceOpenSessionOptions((tenantIdHeaderName, tenantId));
                    await transactionalSession.Open(sessionOptions);

                    var sendOptions = new SendOptions();
                    sendOptions.SetHeader(tenantIdHeaderName, tenantId);
                    sendOptions.RouteToThisEndpoint();

                    await transactionalSession.Send(new SampleMessage(), sendOptions, CancellationToken.None);

                    await transactionalSession.Commit(CancellationToken.None).ConfigureAwait(false);
                }))
                .Done(c => c.MessageReceived)
                .Run();
        }

        static async Task CreateOutboxTable(string tenantId, string endpointName)
        {
            string tablePrefix = TestTableNameCleaner.Clean(endpointName);
            using var connection = MsSqlSystemDataClientConnectionBuilder.MultiTenant.Build(tenantId);
            await connection.OpenAsync().ConfigureAwait(false);

            connection.ExecuteCommand(OutboxScriptBuilder.BuildDropScript(BuildSqlDialect.MsSqlServer), tablePrefix);
            connection.ExecuteCommand(OutboxScriptBuilder.BuildCreateScript(BuildSqlDialect.MsSqlServer), tablePrefix);
        }

        class Context : ScenarioContext, IInjectServiceProvider
        {
            public bool MessageReceived { get; set; }
            public bool CompleteMessageReceived { get; set; }
            public IServiceProvider ServiceProvider { get; set; }
        }

        class AnEndpoint : EndpointConfigurationBuilder
        {
            public AnEndpoint() =>
                EndpointSetup<TransactionSessionWithOutboxEndpoint>(c =>
                {
                    var persistence = c.UsePersistence<SqlPersistence>();
                    persistence.MultiTenantConnectionBuilder(tenantIdHeaderName, tenantId => MsSqlSystemDataClientConnectionBuilder.MultiTenant.Build(tenantId));
                });

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