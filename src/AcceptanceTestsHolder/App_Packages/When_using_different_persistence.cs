using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

[TestFixture]
public class When_using_different_persistence : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_execute_installers()
    {
        // The EndpointsStarted flag is set by acceptance framework
        var context = await Scenario.Define<Context>()
            .WithEndpoint<InMemoryPersistenceEndpoint>()
            .Done(c => c.EndpointsStarted)
            .Run()
            .ConfigureAwait(false);

        // If installers were run, we'd get an System.Exception: "ConnectionBuilder must be defined."
        Assert.True(context.EndpointsStarted);
    }

    public class Context : ScenarioContext
    {
    }

    public class InMemoryPersistenceEndpoint : EndpointConfigurationBuilder
    {
        public InMemoryPersistenceEndpoint()
        {
            EndpointSetup<NoPersistenceServer>(c => c.UsePersistence<InMemoryPersistence>());
        }
    }

    public class StartSagaMessage : IMessage
    {
        public string Property { get; set; }
    }

}