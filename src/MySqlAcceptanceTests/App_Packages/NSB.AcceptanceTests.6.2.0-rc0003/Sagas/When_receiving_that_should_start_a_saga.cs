﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Persistence.Sql;

    public class When_receiving_that_should_start_a_saga : NServiceBusAcceptanceTest
    {
        public class SagaEndpointContext : ScenarioContext
        {
            public bool InterceptingHandlerCalled { get; set; }

            public bool SagaStarted { get; set; }

            public bool InterceptSaga { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(b => b.ExecuteTheseHandlersFirst(typeof(InterceptingHandler)));
            }

            public class TestSaga03 : SqlSaga<TestSaga03.TestSagaData03>, IAmStartedByMessages<StartSagaMessage>
            {
                protected override string CorrelationPropertyName => nameof(TestSagaData03.SomeId);

                public SagaEndpointContext Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Context.SagaStarted = true;
                    Data.SomeId = message.SomeId;
                    return Task.FromResult(0);
                }
                
                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId);
                }

                public class TestSagaData03 : ContainSagaData
                {
                    public virtual string SomeId { get; set; }
                }
            }

            public class InterceptingHandler : IHandleMessages<StartSagaMessage>
            {
                public SagaEndpointContext TestContext { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    TestContext.InterceptingHandlerCalled = true;

                    if (TestContext.InterceptSaga)
                    {
                        context.DoNotContinueDispatchingCurrentMessageToHandlers();
                    }

                    return Task.FromResult(0);
                }
            }
        }


        public class StartSagaMessage : ICommand
        {
            public string SomeId { get; set; }
        }
    }
}