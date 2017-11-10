using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

[TestFixture]
public class When_using_different_persitence : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_execute_installers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<InMemoryPersistenceEndpoint>()
            .Done(c => c.EndpointsStarted)
            .Run()
            .ConfigureAwait(false);

        Assert.True(context.EndpointsStarted);
    }

    public class Context : ScenarioContext
    {
    }

    public class InMemoryPersistenceEndpoint : EndpointConfigurationBuilder
    {
        public InMemoryPersistenceEndpoint()
        {
            EndpointSetup<InMemoryServer>();
        }
    }

    public class StartSagaMessage : IMessage
    {
        public string Property { get; set; }
    }

}