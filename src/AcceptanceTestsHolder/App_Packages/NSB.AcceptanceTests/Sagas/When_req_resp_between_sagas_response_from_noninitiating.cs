namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using NServiceBus.Persistence.Sql;

    public class When_req_resp_between_sagas_response_from_noninitiating : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_autocorrelate_the_response_back()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new InitiateRequestingSaga())))
                .Done(c => c.DidRequestingSagaGetTheResponse)
                .Run();

            Assert.True(context.DidRequestingSagaGetTheResponse);
        }

        public class Context : ScenarioContext
        {
            public bool DidRequestingSagaGetTheResponse { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class RequestingSaga2 : SqlSaga<RequestingSaga2.RequestResponseRequestingSagaData2>,
                IAmStartedByMessages<InitiateRequestingSaga>,
                IHandleMessages<ResponseFromOtherSaga>
            {
                public Context TestContext { get; set; }

                public Task Handle(InitiateRequestingSaga message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new RequestToRespondingSaga
                    {
                        SomeIdThatTheResponseSagaCanCorrelateBackToUs = Data.CorrIdForResponse //wont be needed in the future
                    });
                }

                public Task Handle(ResponseFromOtherSaga message, IMessageHandlerContext context)
                {
                    TestContext.DidRequestingSagaGetTheResponse = true;

                    MarkAsComplete();

                    return Task.FromResult(0);
                }

                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.ConfigureMapping<InitiateRequestingSaga>(m => m.Id);
                    mapper.ConfigureMapping<ResponseFromOtherSaga>(m => m.SomeCorrelationId);
                }

                protected override string CorrelationPropertyName => nameof(RequestResponseRequestingSagaData2.CorrIdForResponse);
                
                public class RequestResponseRequestingSagaData2 : ContainSagaData
                {
                    public virtual Guid CorrIdForResponse { get; set; } //wont be needed in the future
                }
            }

            public class RespondingSaga2 : SqlSaga<RespondingSaga2.RequestResponseRespondingSagaData2>,
                IAmStartedByMessages<RequestToRespondingSaga>,
                IHandleMessages<SendReplyFromNonInitiatingHandler>
            {
                public Context TestContext { get; set; }

                public Task Handle(RequestToRespondingSaga message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new SendReplyFromNonInitiatingHandler
                    {
                        SagaIdSoWeCanCorrelate = Data.CorrIdForRequest
                    });
                }

                public Task Handle(SendReplyFromNonInitiatingHandler message, IMessageHandlerContext context)
                {
                    //reply to originator must be used here since the sender of the incoming message the timeoutmanager and not the requesting saga
                    return ReplyToOriginator(context, new ResponseFromOtherSaga //change this line to Bus.Reply(new ResponseFromOtherSaga  and see it fail
                    {
                        SomeCorrelationId = Data.CorrIdForRequest //wont be needed in the future
                    });
                }

                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.ConfigureMapping<RequestToRespondingSaga>(m => m.SomeIdThatTheResponseSagaCanCorrelateBackToUs);
                    //this line is just needed so we can test the non initiating handler case
                    mapper.ConfigureMapping<SendReplyFromNonInitiatingHandler>(m => m.SagaIdSoWeCanCorrelate);
                }

                protected override string CorrelationPropertyName => nameof(RequestResponseRespondingSagaData2.CorrIdForRequest);

                public class RequestResponseRespondingSagaData2 : ContainSagaData
                {
                    public virtual Guid CorrIdForRequest { get; set; }
                }
            }
        }

        public class InitiateRequestingSaga : ICommand
        {
            public InitiateRequestingSaga()
            {
                Id = Guid.NewGuid();
            }

            public Guid Id { get; set; }
        }

        public class RequestToRespondingSaga : ICommand
        {
            public Guid SomeIdThatTheResponseSagaCanCorrelateBackToUs { get; set; }
        }

        public class ResponseFromOtherSaga : IMessage
        {
            public Guid SomeCorrelationId { get; set; }
        }

        public class SendReplyFromNonInitiatingHandler : ICommand
        {
            public Guid SagaIdSoWeCanCorrelate { get; set; }
        }
    }
}