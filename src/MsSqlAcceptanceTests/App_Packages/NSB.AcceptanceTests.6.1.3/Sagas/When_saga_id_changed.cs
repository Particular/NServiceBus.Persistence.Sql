namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using EndpointTemplates;
    using NUnit.Framework;
    using Persistence.Sql;

    [TestFixture]
    public class When_saga_id_changed : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw()
        {
            var exception = Assert.ThrowsAsync<MessagesFailedException>(async () =>
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(
                        b => b.When(session => session.SendLocal(new StartSaga
                        {
                            DataId = Guid.NewGuid()
                        })))
                    .Done(c => c.FailedMessages.Any())
                    .Run());

            Assert.That(exception.FailedMessages, Has.Count.EqualTo(1));
            var failedMessage = exception.FailedMessages.Single();
            Assert.That(((Context) exception.ScenarioContext).MessageId, Is.EqualTo(failedMessage.MessageId), "Message should be moved to errorqueue");
            Assert.That(failedMessage.Exception.Message, Contains.Substring("A modification of IContainSagaData.Id has been detected. This property is for infrastructure purposes only and should not be modified. SagaType:"));
        }

        public class Context : ScenarioContext
        {
            public string MessageId { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            [CorrelatedSaga(correlationProperty: nameof(SagaIdChangedSagaData.DataId))]
            public class SagaIdChangedSaga : SqlSaga<SagaIdChangedSaga.SagaIdChangedSagaData>,
                IAmStartedByMessages<StartSaga>
            {
                public Context TestContext { get; set; }

                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.Id = Guid.NewGuid();
                    TestContext.MessageId = context.MessageId;
                    return Task.FromResult(0);
                }

                protected override void ConfigureMapping(MessagePropertyMapper<SagaIdChangedSagaData> mapper)
                {
                    mapper.MapMessage<StartSaga>(m => m.DataId);
                }

                public class SagaIdChangedSagaData : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }
            }
        }

        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}