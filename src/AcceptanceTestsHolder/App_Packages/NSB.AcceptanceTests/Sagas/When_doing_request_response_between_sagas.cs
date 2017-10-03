namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Persistence.Sql;

    public partial class When_doing_request_response_between_sagas : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_autocorrelate_the_response_back_to_the_requesting_saga()
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

            public class RequestResponseRequestingSaga1 : SqlSaga<RequestResponseRequestingSaga1.RequestResponseRequestingSagaData1>,
                IAmStartedByMessages<InitiateRequestingSaga>,
                IHandleMessages<ResponseFromOtherSaga>
            {
                public Context TestContext { get; set; }

                public Task Handle(InitiateRequestingSaga message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new RequestToRespondingSaga
                    {
                        SomeId = Guid.NewGuid()
                    });
                }

                protected override string CorrelationPropertyName => nameof(RequestResponseRequestingSagaData1.CorrIdForResponse);

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

                public class RequestResponseRequestingSagaData1 : ContainSagaData
                {
                    public virtual Guid CorrIdForResponse { get; set; }
                }
            }

            public class RequestResponseRespondingSaga1 : SqlSaga<RequestResponseRespondingSaga1.RequestResponseRespondingSagaData1>,
                IAmStartedByMessages<RequestToRespondingSaga>
            {
                public Context TestContext { get; set; }

                public Task Handle(RequestToRespondingSaga message, IMessageHandlerContext context)
                {
                    // Both reply and reply to originator work here since the sender of the incoming message is the requesting saga
                    // we explicitly set the correlation ID to a non-existent saga since auto correlation happens to work for this special case
                    // where we reply from the first handler
                    return context.Reply(new ResponseFromOtherSaga{SomeCorrelationId = Guid.NewGuid()});
                }

                protected override string CorrelationPropertyName => nameof(RequestResponseRespondingSagaData1.CorrIdForRequest);
                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.ConfigureMapping<RequestToRespondingSaga>(m => m.SomeId);
                }

                public class RequestResponseRespondingSagaData1 : ContainSagaData
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
            public Guid SomeId { get; set; }
        }

        public class ResponseFromOtherSaga : IMessage
        {
            public Guid SomeCorrelationId { get; set; }
        }
    }
}
