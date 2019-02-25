﻿using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class When_using_multi_tenant : NServiceBusAcceptanceTest
{
    [OneTimeSetUp]
    public void SetUpTenantDatabases()
    {
        MsSqlConnectionBuilder.MultiTenant.Setup("TenantA");
        MsSqlConnectionBuilder.MultiTenant.Setup("TenantB");
    }

    [OneTimeTearDown]
    public void TearDownTenantDatabases()
    {
        MsSqlConnectionBuilder.MultiTenant.TearDown("TenantA");
        MsSqlConnectionBuilder.MultiTenant.TearDown("TenantB");
    }

    [Test]
    public void Should_not_run_if_Outbox_cleanup_enabled()
    {
        var exception = Assert.ThrowsAsync<Exception>(async () =>
        {
            await Scenario.Define<Context>()
                .WithEndpoint<MultiTenantHandlerEndpoint>(b => b.CustomConfig(c => ConfigureMultiTenant(c, true, true)))
                .Done(c => c.EndpointsStarted)
                .Run()
                .ConfigureAwait(false);
        });

        var msg = exception.Message;

        Assert.That(msg.Contains("EnableOutbox") && msg.Contains("DisableCleanup"));
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

    private async Task RunTest<TEndpointType>(bool useOutbox)
        where TEndpointType : EndpointConfigurationBuilder
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<TEndpointType>(b =>
            {
                b.CustomConfig((cfg, ctx) => ConfigureMultiTenant(cfg, useOutbox, false));
                SendMultiTenantMessages(b);
            })
            .Done(c => c.TenantADbName != null && c.TenantBDbName != null)
            .Run(TimeSpan.FromSeconds(30))
            .ConfigureAwait(false);

        Assert.AreEqual("nservicebus_tenanta", context.TenantADbName);
        Assert.AreEqual("nservicebus_tenantb", context.TenantBDbName);

        context.Cleanup();
    }

    static void ConfigureMultiTenant(EndpointConfiguration c, bool useOutbox = true, bool cleanOutbox = true)
    {
        // Settings already configured with a normal connection builder for subscriptions/timeouts
        // this is expected and required if using a transport without pubsub and timeouts built in

        var persistence = c.UsePersistence<SqlPersistence>();
        persistence.MultiTenantConnectionBuilder("TenantId", tenantId => MsSqlConnectionBuilder.MultiTenant.Build(tenantId));

        if (useOutbox)
        {
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

    static void SendMultiTenantMessages(EndpointBehaviorBuilder<Context> builder)
    {
        EndpointConfiguration cfg = null;

        builder.CustomConfig(c => cfg = c);

        builder.When(async (session, context) =>
        {
            var tablePrefix = cfg.GetSettings().EndpointName().Replace(".", "_");
            MsSqlConnectionBuilder.MultiTenant.Setup("TenantA");
            MsSqlConnectionBuilder.MultiTenant.Setup("TenantB");
            var helperA = new ConfigureEndpointHelper(cfg, tablePrefix, () => MsSqlConnectionBuilder.MultiTenant.Build("TenantA"), BuildSqlDialect.MsSqlServer, null);
            var helperB = new ConfigureEndpointHelper(cfg, tablePrefix, () => MsSqlConnectionBuilder.MultiTenant.Build("TenantB"), BuildSqlDialect.MsSqlServer, null);
            context.Cleanup = () =>
            {
                helperA.Cleanup();
                helperB.Cleanup();
            };

            await SendTenantMessage(session, "TenantA");
            await SendTenantMessage(session, "TenantB");
        });
    }

    static Task SendTenantMessage(IMessageSession session, string tenantId)
    {
        var sendOptions = new SendOptions();
        sendOptions.SetHeader("TenantId", tenantId);
        sendOptions.RouteToThisEndpoint();
        return session.Send(new TestMessage { TenantId = tenantId}, sendOptions);
    }

    public class Context : ScenarioContext
    {
        // The EndpointsStarted flag is set by acceptance framework
        public string TenantADbName { get; set; }
        public string TenantBDbName { get; set; }
        internal Action Cleanup { get; set; }
    }

    public class MultiTenantHandlerEndpoint : EndpointConfigurationBuilder
    {
        public MultiTenantHandlerEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        public class TestHandler : IHandleMessages<TestMessage>
        {
            Context testContext;

            public TestHandler(Context testContext)
            {
                this.testContext = testContext;
            }

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

                return Task.FromResult(0);
            }
        }
    }

    public class MultiTenantSagaEndpoint : EndpointConfigurationBuilder
    {
        public MultiTenantSagaEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        public class TestSaga : Saga<TestSaga.TestSagaData>,
            IAmStartedByMessages<TestMessage>
        {
            Context testContext;

            public TestSaga(Context testContext)
            {
                this.testContext = testContext;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
            {
                mapper.ConfigureMapping<TestMessage>(msg => msg.TenantId).ToSaga(saga => saga.TenantId);
            }

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

                return Task.FromResult(0);
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