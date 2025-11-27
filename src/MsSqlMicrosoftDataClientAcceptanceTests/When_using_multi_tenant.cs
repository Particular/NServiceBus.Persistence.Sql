using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class When_using_multi_tenant : NServiceBusAcceptanceTest
{
    [OneTimeSetUp]
    public void SetUpTenantDatabases()
    {
        MsSqlMicrosoftDataClientConnectionBuilder.MultiTenant.Setup("TenantA");
        MsSqlMicrosoftDataClientConnectionBuilder.MultiTenant.Setup("TenantB");
    }

    [OneTimeTearDown]
    public void TearDownTenantDatabases()
    {
        MsSqlMicrosoftDataClientConnectionBuilder.MultiTenant.TearDown("TenantA");
        MsSqlMicrosoftDataClientConnectionBuilder.MultiTenant.TearDown("TenantB");
    }

    [Test]
    public void Should_not_run_if_Outbox_cleanup_enabled()
    {
        var exception = Assert.ThrowsAsync<Exception>(async () =>
        {
            await Scenario.Define<Context>()
                .WithEndpoint<MultiTenantHandlerEndpoint>(b => b.CustomConfig(c => ConfigureMultiTenant(c, true, true)))
                .Done(c => c.EndpointsStarted)
                .Run();
        });

        var msg = exception.Message;

        Assert.That(msg.Contains("EnableOutbox") && msg.Contains("DisableCleanup"));
    }

    [Test]
    public async Task Should_throw_if_no_tenant_id()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<MultiTenantHandlerEndpoint>(b =>
            {
                b.DoNotFailOnErrorMessages();
                b.CustomConfig(c => ConfigureMultiTenant(c, true, false));
                b.When(session => session.SendLocal(new TestMessage { TenantId = "TenantIdButNotInHeader" }));
            })
            .Done(c => c.FailedMessages.Any())
            .Run();

        var failed = context.FailedMessages.Values.FirstOrDefault()?.FirstOrDefault();
        var exception = failed.Exception;
        var msg = exception.Message;

        Assert.That(msg, Does.Contain("unable to determine the tenant id"));
    }

    [Test]
    public async Task Handler_with_Outbox_enabled()
    {
        await RunTest<MultiTenantHandlerEndpoint>(true);
    }

    [Test]
    public async Task Saga_with_Outbox_enabled()
    {
        await RunTest<MultiTenantSagaEndpoint>(true);
    }

    [Test]
    public async Task Handler_with_Outbox_disabled()
    {
        await RunTest<MultiTenantHandlerEndpoint>(false);
    }

    [Test]
    public async Task Saga_with_Outbox_disabled()
    {
        await RunTest<MultiTenantSagaEndpoint>(false);
    }

    async Task RunTest<TEndpointType>(bool useOutbox)
        where TEndpointType : EndpointConfigurationBuilder, new()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TEndpointType>(b =>
            {
                b.CustomConfig((cfg, ctx) => ConfigureMultiTenant(cfg, useOutbox, false));
                SetupTenantDatabases(b);
                b.When(async session =>
                {
                    await SendTenantMessage(session, "TenantA");
                    await SendTenantMessage(session, "TenantB");
                });
            })
            .Done(c => c.TenantADbName != null && c.TenantBDbName != null)
            .Run(TimeSpan.FromSeconds(30));

        Assert.Multiple(() =>
        {
            Assert.That(context.TenantADbName, Is.EqualTo("nservicebus_tenanta"));
            Assert.That(context.TenantBDbName, Is.EqualTo("nservicebus_tenantb"));
        });
    }

    [Test]
    public async Task Use_multiple_tenant_headers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<MultiTenantSagaEndpoint>(b =>
            {
                b.CustomConfig((cfg, ctx) =>
                {
                    var captureTenantId = new Func<IncomingMessage, string>(msg =>
                    {
                        if (msg.Headers.TryGetValue("TenantHeader1", out var tenantId) || msg.Headers.TryGetValue("TenantHeader2", out tenantId))
                        {
                            return tenantId;
                        }

                        return null;
                    });

                    var persistence = cfg.UsePersistence<SqlPersistence>();
                    persistence.MultiTenantConnectionBuilder(captureTenantId, tenantId => MsSqlMicrosoftDataClientConnectionBuilder.MultiTenant.Build(tenantId));
                    cfg.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                    cfg.EnableOutbox().DisableCleanup();
                });
                SetupTenantDatabases(b);
                b.When(async session =>
                {
                    await SendTenantMessage(session, "TenantA", "TenantHeader1");
                    await SendTenantMessage(session, "TenantB", "TenantHeader2");
                });
            })
            .Done(c => c.TenantADbName != null && c.TenantBDbName != null)
            .Run(TimeSpan.FromSeconds(30));

        Assert.Multiple(() =>
        {
            Assert.That(context.TenantADbName, Is.EqualTo("nservicebus_tenanta"));
            Assert.That(context.TenantBDbName, Is.EqualTo("nservicebus_tenantb"));
        });
    }

    static void ConfigureMultiTenant(EndpointConfiguration c, bool useOutbox = true, bool cleanOutbox = true)
    {
        // Settings already configured with a normal connection builder for subscriptions/timeouts
        // this is expected and required if using a transport without pubsub and timeouts built in
        var persistence = c.UsePersistence<SqlPersistence>();
        persistence.MultiTenantConnectionBuilder("TenantId", tenantId => MsSqlMicrosoftDataClientConnectionBuilder.MultiTenant.Build(tenantId));

        if (useOutbox)
        {
            c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
            var outbox = c.EnableOutbox();
            if (!cleanOutbox)
            {
                outbox.DisableCleanup();
            }
        }
        else
        {
            c.DisableFeature<Outbox>();
        }
    }

    static void SetupTenantDatabases(EndpointBehaviorBuilder<Context> builder)
    {
        EndpointConfiguration cfg = null;
        SetupAndTeardownDatabase tenantASetupFeature = null;
        SetupAndTeardownDatabase tenantBSetupFeature = null;

        builder.CustomConfig(c =>
        {
            cfg = c;

            var tablePrefix = cfg.GetSettings().EndpointName().Replace(".", "_");
            MsSqlMicrosoftDataClientConnectionBuilder.MultiTenant.Setup("TenantA");
            MsSqlMicrosoftDataClientConnectionBuilder.MultiTenant.Setup("TenantB");

            cfg.RegisterStartupTask(sp =>
            {
                tenantASetupFeature = new SetupAndTeardownDatabase(
                    TestContext.CurrentContext.Test.ID,
                    cfg.GetSettings(), tablePrefix,
                    () => MsSqlMicrosoftDataClientConnectionBuilder.MultiTenant.Build("TenantA"),
                    BuildSqlDialect.MsSqlServer);

                return tenantASetupFeature;
            });
            cfg.RegisterStartupTask(sp =>
            {
                tenantBSetupFeature = new SetupAndTeardownDatabase(
                    TestContext.CurrentContext.Test.ID,
                    cfg.GetSettings(), tablePrefix,
                    () => MsSqlMicrosoftDataClientConnectionBuilder.MultiTenant.Build("TenantB"),
                    BuildSqlDialect.MsSqlServer);

                return tenantBSetupFeature;
            });
        });

        builder.When((_, context) =>
        {
            context.Cleanup = async () =>
            {
                if (tenantASetupFeature != null)
                {
                    await tenantASetupFeature.ManualStop(CancellationToken.None);
                }

                if (tenantBSetupFeature != null)
                {
                    await tenantBSetupFeature.ManualStop(CancellationToken.None);
                }
            };

            return Task.CompletedTask;
        });
    }

    static Task SendTenantMessage(IMessageSession session, string tenantId, string tenantHeaderName = "TenantId")
    {
        var sendOptions = new SendOptions();
        sendOptions.SetHeader(tenantHeaderName, tenantId);
        sendOptions.RouteToThisEndpoint();
        return session.Send(new TestMessage { TenantId = tenantId }, sendOptions);
    }

    public class Context : ScenarioContext
    {
        // The EndpointsStarted flag is set by acceptance framework
        public string TenantADbName { get; set; }
        public string TenantBDbName { get; set; }
        internal Func<Task> Cleanup { get; set; }
    }

    public class MultiTenantHandlerEndpoint : EndpointConfigurationBuilder
    {
        public MultiTenantHandlerEndpoint() => EndpointSetup<DefaultServer>();

        public class TestHandler(Context testContext) : IHandleMessages<TestMessage>
        {
            public Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                var session = context.SynchronizedStorageSession.SqlPersistenceSession();
                var dbName = session.Connection.Database;

                if (message.TenantId == "TenantA")
                {
                    testContext.TenantADbName = dbName;
                }
                else if (message.TenantId == "TenantB")
                {
                    testContext.TenantBDbName = dbName;
                }

                return Task.CompletedTask;
            }
        }
    }

    public class MultiTenantSagaEndpoint : EndpointConfigurationBuilder
    {
        public MultiTenantSagaEndpoint() => EndpointSetup<DefaultServer>();

        public class TestSaga(Context testContext) : Saga<TestSaga.TestSagaData>,
            IAmStartedByMessages<TestMessage>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper) => mapper.MapSaga(s => s.TenantId).ToMessage<TestMessage>(msg => msg.TenantId);

            public Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                var session = context.SynchronizedStorageSession.SqlPersistenceSession();
                var dbName = session.Connection.Database;

                if (message.TenantId == "TenantA")
                {
                    testContext.TenantADbName = dbName;
                }
                else if (message.TenantId == "TenantB")
                {
                    testContext.TenantBDbName = dbName;
                }

                return Task.CompletedTask;
            }

            public class TestSagaData : ContainSagaData
            {
                public string TenantId { get; set; }
            }
        }
    }

    public class TestMessage : IMessage
    {
        public string TenantId { get; set; }
    }
}