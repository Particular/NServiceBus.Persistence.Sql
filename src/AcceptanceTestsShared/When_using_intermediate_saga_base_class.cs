using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

[TestFixture]
public class When_using_intermediate_saga_base_class : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_work()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<CoreSagaBaseClassEndpoint>(c => { c.When(session => session.SendLocal(new StartSaga { Correlation = "Corr" })); })
            .Done(c => c.Received)
            .Run();

        Assert.That(context.Received, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool Received { get; set; }
    }

    public class CoreSagaBaseClassEndpoint : EndpointConfigurationBuilder
    {
        public CoreSagaBaseClassEndpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.Sagas().DisableBestPracticeValidation();
            });

        public class CoreSagaWithBase(Context testContext) : BaseSaga
        {
            public override Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                Data.Correlation = message.Correlation;
                testContext.Received = true;
                return Task.CompletedTask;
            }
        }

        public class BaseSaga : Saga<BaseSaga.SagaData>,
            IAmStartedByMessages<StartSaga>
        {
            public class SagaData : ContainSagaData
            {
                public string Correlation { get; set; }
            }

            protected sealed override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) =>
                mapper.MapSaga(s => s.Correlation)
                    .ToMessage<StartSaga>(s => s.Correlation);

            public virtual Task Handle(StartSaga message, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    public class StartSaga : ICommand
    {
        public string Correlation { get; set; }
    }
}