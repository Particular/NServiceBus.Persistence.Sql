namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    public class When_using_transactional_session_with_transactionscope_sql_transport : NServiceBusAcceptanceTest
    {
        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            MsSqlMicrosoftDataClientConnectionBuilder.MultiTenant.Setup("Transport");

            MsSqlMicrosoftDataClientConnectionBuilder.DropDbIfCollationIncorrect();
            MsSqlMicrosoftDataClientConnectionBuilder.CreateDbIfNotExists();

            await OutboxHelpers.CreateDataTable();
        }

        [OneTimeTearDown]
        public void TearDownTenantDatabases()
        {
            MsSqlMicrosoftDataClientConnectionBuilder.MultiTenant.TearDown("Transport");
        }

        [Test]
        public async Task Should_not_escalate_to_DTC()
        {
            await OutboxHelpers.CreateOutboxTable<AnEndpoint>();

            string rowId = Guid.NewGuid().ToString();

            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using IServiceScope scope = ctx.ServiceProvider.CreateScope();
                    using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();
                    var sessionOptions = new SqlPersistenceOpenSessionOptions();
                    await transactionalSession.Open(sessionOptions);

                    await transactionalSession.SendLocal(new SampleMessage());

                    //Ensure storage session is available
                    var storageSession = transactionalSession.SynchronizedStorageSession.SqlPersistenceSession();

                    await transactionalSession.Commit();
                }))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.That(ctx.MessageReceived, Is.True);
        }

        class Context : ScenarioContext, IInjectServiceProvider
        {
            public bool MessageReceived { get; set; }
            public IServiceProvider ServiceProvider { get; set; }
        }

        class AnEndpoint : EndpointConfigurationBuilder
        {
            public AnEndpoint() => EndpointSetup<TransactionSessionWithSqlTransportOutboxEndpoint>(c => c.EnableOutbox().UseTransactionScope());

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
        }

        class SampleMessage : ICommand
        {
        }
    }
}