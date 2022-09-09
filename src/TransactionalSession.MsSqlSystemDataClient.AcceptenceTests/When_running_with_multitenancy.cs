﻿namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Persistence.Sql;

public class When_running_with_multitenancy : NServiceBusAcceptanceTest
{
    static readonly string tenantId = "aTenant";
    static readonly string tenantIdHeaderName = "TenantName";

    [OneTimeSetUp]
    public void SetUpTenantDatabases()
    {
        MsSqlSystemDataClientConnectionBuilder.MultiTenant.Setup(tenantId, true);
    }

    [OneTimeTearDown]
    public void TearDownTenantDatabases()
    {
        MsSqlSystemDataClientConnectionBuilder.MultiTenant.TearDown(tenantId, true);
    }

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

                var sessionOptions = new SqlPersistenceOpenSessionOptions(tenantIdHeaderName, tenantId);
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

    public static async Task CreateOutboxTable(string tenantId, string endpointName)
    {
        string tablePrefix = $"{endpointName.Replace('.', '_')}_";
        var dialect = new SqlDialect.MsSqlServer();

        string scriptDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NServiceBus.Persistence.Sql",
            dialect.GetType().Name);

        await ScriptRunner.Install(dialect, tablePrefix, () => MsSqlSystemDataClientConnectionBuilder.MultiTenant.Build(tenantId, true), scriptDirectory, true, false, false,
            CancellationToken.None).ConfigureAwait(false);
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
            EndpointSetup<TransactionSessionWithOutboxEndpoint>();

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