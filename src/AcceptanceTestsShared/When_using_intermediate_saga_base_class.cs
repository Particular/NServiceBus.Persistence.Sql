using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;

[TestFixture]
public class When_using_intermediate_saga_base_class : NServiceBusAcceptanceTest
{
    [Test]
    public void Ensure_Core_Saga_will_throw()
    {
        PerformTestOn<CoreSagaBaseClassEndpoint>();
    }

    [Test]
    public void Ensure_SqlSaga_will_throw()
    {
        PerformTestOn<SqlSagaBaseClassEndpoint>();
    }

    void PerformTestOn<TEndpointSelection>()
        where TEndpointSelection : EndpointConfigurationBuilder
    {
        var ex = Assert.ThrowsAsync<Exception>(async () =>
        {
            await Scenario.Define<Context>()
                .WithEndpoint<TEndpointSelection>(c => { c.When(session => session.SendLocal(new StartSaga { Correlation = "Corr" })); })
                .Done(c => c.EndpointsStarted)
                .Run()
                .ConfigureAwait(false);
        });

        Assert.IsTrue(ex.Message == "Saga implementations must inherit from either Saga<T> or SqlSaga<T> directly. Deep class hierarchies are not supported.");
    }

    public class Context : ScenarioContext
    {
    }

    public class CoreSagaBaseClassEndpoint : EndpointConfigurationBuilder
    {
        public CoreSagaBaseClassEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        public class CoreSagaWithBase : BaseSaga
        {
            public class SagaData : ContainSagaData
            {
                public string Correlation { get; set; }
            }

            public override Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                return Task.FromResult(0);
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
                return Task.FromResult(0);
            }
        }

    }

    public class SqlSagaBaseClassEndpoint : EndpointConfigurationBuilder
    {
        public SqlSagaBaseClassEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        public class SqlSagaWithBase : BaseSqlSaga,
            IAmStartedByMessages<StartSaga>
        {
            public class SagaData : ContainSagaData
            {
                public string Correlation { get; set; }
            }

            protected override void ConfigureMapping(IMessagePropertyMapper mapper)
            {
                base.ConfigureMapping(mapper);
                mapper.ConfigureMapping<StartSaga>(msg => msg.Correlation);
            }

            public Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                return Task.FromResult(0);
            }
        }

        public class BaseSqlSaga : SqlSaga<SqlSagaWithBase.SagaData>
        {
            protected override string CorrelationPropertyName => "Correlation";

            protected override void ConfigureMapping(IMessagePropertyMapper mapper)
            {

            }
        }
    }

    public class StartSaga : ICommand
    {
        public string Correlation { get; set; }
    }

}