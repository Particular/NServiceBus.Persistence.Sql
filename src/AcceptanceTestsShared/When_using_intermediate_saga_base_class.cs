using System;
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
    public void Ensure_Core_Saga_will_throw()
    {
        PerformTestOn<CoreSagaBaseClassEndpoint>();
    }

    void PerformTestOn<TEndpointSelection>()
        where TEndpointSelection : EndpointConfigurationBuilder, new()
    {
        var ex = Assert.ThrowsAsync<Exception>(async () =>
        {
            await Scenario.Define<Context>()
                .WithEndpoint<TEndpointSelection>(c => { c.When(session => session.SendLocal(new StartSaga { Correlation = "Corr" })); })
                .Done(c => c.EndpointsStarted)
                .Run()
                .ConfigureAwait(false);
        });

        Assert.That(ex.Message == "Saga implementations must inherit from either Saga<T> or SqlSaga<T> directly. Deep class hierarchies are not supported.", Is.True);
    }

    public class Context : ScenarioContext
    {
    }

    public class CoreSagaBaseClassEndpoint : EndpointConfigurationBuilder
    {
        public CoreSagaBaseClassEndpoint()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.Sagas().DisableBestPracticeValidation();
            });
        }

        public class CoreSagaWithBase : BaseSaga
        {
            public class SagaData : ContainSagaData
            {
                public string Correlation { get; set; }
            }

            public override Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                return Task.CompletedTask;
            }
        }

        public class BaseSaga : Saga<CoreSagaWithBase.SagaData>,
            IAmStartedByMessages<StartSaga>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CoreSagaWithBase.SagaData> mapper)
            {
                mapper.ConfigureMapping<StartSaga>(msg => msg.Correlation).ToSaga(saga => saga.Correlation);
            }


            public virtual Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                return Task.CompletedTask;
            }
        }

    }

    public class StartSaga : ICommand
    {
        public string Correlation { get; set; }
    }

}