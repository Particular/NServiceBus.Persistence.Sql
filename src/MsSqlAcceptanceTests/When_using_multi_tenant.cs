using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
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
                .WithEndpoint<EndpointWithOutboxCleanupEnabled>()
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
        await Scenario.Define<Context>()
            .WithEndpoint<MultiTenantEndpoint>()
            .Done(c => c.EndpointsStarted)
            .Run()
            .ConfigureAwait(false);
    }

    [Test]
    public async Task Should_run_when_Outbox_is_disabled()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<MultiTenantEndpointNoOutbox>()
            .Done(c => c.EndpointsStarted)
            .Run()
            .ConfigureAwait(false);
    }

    public class Context : ScenarioContext
    {
        // The EndpointsStarted flag is set by acceptance framework
    }

    public class EndpointWithOutboxCleanupEnabled : EndpointConfigurationBuilder
    {
        public EndpointWithOutboxCleanupEnabled()
        {
            EndpointSetup<DefaultServer>(c => ConfigureMultiTenant(c, true, true));
        }
    }

    public class MultiTenantEndpoint : EndpointConfigurationBuilder
    {
        public MultiTenantEndpoint()
        {
            EndpointSetup<DefaultServer>(c => ConfigureMultiTenant(c, true, false));
        }
    }

    public class MultiTenantEndpointNoOutbox : EndpointConfigurationBuilder
    {
        public MultiTenantEndpointNoOutbox()
        {
            EndpointSetup<DefaultServer>(c => ConfigureMultiTenant(c, false));
        }
    }

    static void ConfigureMultiTenant(EndpointConfiguration c, bool useOutbox = true, bool cleanOutbox = true)
    {
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



    //public class OutboxEndpointWithSagasDisabled : EndpointConfigurationBuilder
    //{
    //    public OutboxEndpointWithSagasDisabled()
    //    {
    //        EndpointSetup<DefaultServer>(c =>
    //        {
    //            c.DisableFeature<Sagas>();
    //            c.EnableOutbox();
    //        });
    //    }
    //}

    //public class StartSagaMessage : IMessage
    //{
    //    public string Property { get; set; }
    //}

}