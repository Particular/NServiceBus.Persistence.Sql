using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Features;
using NUnit.Framework;

[TestFixture]
public class When_using_outbox_but_no_sagas : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_be_able_to_start_the_endpoint()
    {
        // The EndpointsStarted flag is set by acceptance framework
        var context = await Scenario.Define<Context>()
            .WithEndpoint<OutboxEndpointWithSagasDisabled>()
            .Done(c => c.EndpointsStarted)
            .Run();

        Assert.That(context.EndpointsStarted, Is.True);
    }

    public class Context : ScenarioContext
    {
    }

    public class OutboxEndpointWithSagasDisabled : EndpointConfigurationBuilder
    {
        public OutboxEndpointWithSagasDisabled()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.DisableFeature<Sagas>();
                c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                c.EnableOutbox();
            });
        }
    }

    public class StartSagaMessage : IMessage
    {
        public string Property { get; set; }
    }

}