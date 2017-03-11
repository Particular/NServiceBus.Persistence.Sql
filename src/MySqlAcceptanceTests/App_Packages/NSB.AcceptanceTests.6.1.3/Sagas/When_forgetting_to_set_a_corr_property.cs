namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Persistence.Sql;

    public class When_forgetting_to_set_a_corr_property : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_matter()
        {
            var id = Guid.NewGuid();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<NullPropertyEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage
                {
                    SomeId = id
                })))
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual(context.SomeId, id);
        }

        public class Context : ScenarioContext
        {
            public Guid SomeId { get; set; }
            public bool Done { get; set; }
        }

        public class NullPropertyEndpoint : EndpointConfigurationBuilder
        {
            public NullPropertyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            [CorrelatedSaga(correlationProperty: nameof(NullCorrPropertySagaData.SomeId))]
            public class NullCorrPropertySaga : SqlSaga<NullCorrPropertySagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    //oops I forgot Data.SomeId = message.SomeId
                    if (message.SecondMessage)
                    {
                        Context.SomeId = Data.SomeId;
                        Context.Done = true;
                        return Task.FromResult(0);
                    }

                    return context.SendLocal(new StartSagaMessage
                    {
                        SomeId = message.SomeId,
                        SecondMessage = true
                    });
                }

                protected override void ConfigureMapping(MessagePropertyMapper<NullCorrPropertySagaData> mapper)
                {
                    mapper.MapMessage<StartSagaMessage>(m => m.SomeId);
                }
            }

            public class NullCorrPropertySagaData : IContainSagaData
            {
                public virtual Guid SomeId { get; set; }
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
            public bool SecondMessage { get; set; }
        }
    }
}