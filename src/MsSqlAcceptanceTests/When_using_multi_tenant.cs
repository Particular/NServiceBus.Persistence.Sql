using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;

[TestFixture]
public class When_using_multi_tenant : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_not_run_if_Outbox_cleanup_enabled()
    {
        var exception = Assert.ThrowsAsync<Exception>(async () =>
        {
            await Scenario.Define<Context>()
                .WithEndpoint<MultiTenantEndpoint>(b => b.CustomConfig(c => ConfigureMultiTenant(c, true, true)))
                .Done(c => c.EndpointsStarted)
                .Run()
                .ConfigureAwait(false);
        });

        var msg = exception.Message;

        Assert.That(msg.Contains("EnableOutbox") && msg.Contains("DisableCleanup"));
    }

    [Test]
    public async Task Should_run_when_Outbox_cleanup_disabled()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<MultiTenantEndpoint>(b =>
            {
                b.CustomConfig(c => ConfigureMultiTenant(c, true, false));
                b.When(async session =>
                {
                    await SendTenantMessage<HandlerMessage>(session, "TenantA");
                    await SendTenantMessage<HandlerMessage>(session, "TenantB");
                    await SendTenantMessage<SagaMessage>(session, "TenantA");
                    await SendTenantMessage<SagaMessage>(session, "TenantB");
                });
            })
            .Done(c => c.TenantADbName != null && c.TenantBDbName != null && c.SagaTenantADbName != null && c.SagaTenantBDbName != null)
            .Run(TimeSpan.FromSeconds(30))
            .ConfigureAwait(false);

        Assert.AreEqual("nservicebus", context.TenantADbName);
        Assert.AreEqual("nservicebus", context.TenantBDbName);
        Assert.AreEqual("nservicebus", context.SagaTenantADbName);
        Assert.AreEqual("nservicebus", context.SagaTenantBDbName);
    }

    [Test]
    public async Task Should_run_when_Outbox_is_disabled()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<MultiTenantEndpoint>(b =>
            {
                b.CustomConfig(c => ConfigureMultiTenant(c, false));
                b.When(async session =>
                {
                    await SendTenantMessage<HandlerMessage>(session, "TenantA");
                    await SendTenantMessage<HandlerMessage>(session, "TenantB");
                    await SendTenantMessage<SagaMessage>(session, "TenantA");
                    await SendTenantMessage<SagaMessage>(session, "TenantB");
                });
            })
            .Done(c => c.TenantADbName != null && c.TenantBDbName != null && c.SagaTenantADbName != null && c.SagaTenantBDbName != null)
            .Run(TimeSpan.FromSeconds(30))
            .ConfigureAwait(false);

        Assert.AreEqual("nservicebus", context.TenantADbName);
        Assert.AreEqual("nservicebus", context.TenantBDbName);
        Assert.AreEqual("nservicebus", context.SagaTenantADbName);
        Assert.AreEqual("nservicebus", context.SagaTenantBDbName);
    }

    static void ConfigureMultiTenant(EndpointConfiguration c, bool useOutbox = true, bool cleanOutbox = true)
    {
        // Undo the default call to ConnectionBuilder
        c.GetSettings().Set("SqlPersistence.ConnectionManager", null);

        var persistence = c.UsePersistence<SqlPersistence>();
        persistence.MultiTenantConnectionBuilder("TenantId", tenantId => MsSqlConnectionBuilder.Build());

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

    Task SendTenantMessage<T>(IMessageSession session, string tenantId) where T : TestMessage, new()
    {
        var sendOptions = new SendOptions();
        sendOptions.SetHeader("TenantId", tenantId);
        sendOptions.RouteToThisEndpoint();
        return session.Send(new T {TenantId = tenantId}, sendOptions);
    }

    public class Context : ScenarioContext
    {
        // The EndpointsStarted flag is set by acceptance framework
        public string TenantADbName { get; set; }
        public string TenantBDbName { get; set; }
        public string SagaTenantADbName { get; set; }
        public string SagaTenantBDbName { get; set; }
    }

    public class MultiTenantEndpoint : EndpointConfigurationBuilder
    {
        public MultiTenantEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        public class TestHandler : IHandleMessages<HandlerMessage>
        {
            Context testContext;

            public TestHandler(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(HandlerMessage message, IMessageHandlerContext context)
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

        public class TestSaga : Saga<TestSaga.TestSagaData>,
            IAmStartedByMessages<SagaMessage>
        {
            Context testContext;

            public TestSaga(Context testContext)
            {
                this.testContext = testContext;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
            {
                mapper.ConfigureMapping<SagaMessage>(msg => msg.TenantId).ToSaga(saga => saga.TenantId);
            }

            public Task Handle(SagaMessage message, IMessageHandlerContext context)
            {
                var session = context.SynchronizedStorageSession.SqlPersistenceSession();
                var dbName = session.Connection.Database;

                if (message.TenantId == "TenantA")
                {
                    testContext.SagaTenantADbName = dbName;
                }
                else if (message.TenantId == "TenantB")
                {
                    testContext.SagaTenantBDbName = dbName;
                }

                return Task.FromResult(0);
            }

            public class TestSagaData : ContainSagaData
            {
                public string TenantId { get; set; }
            }
        }
    }


    public class HandlerMessage : TestMessage { }
    public class SagaMessage : TestMessage { }

    public abstract class TestMessage : IMessage
    {
        public string TenantId { get; set; }
    }

}