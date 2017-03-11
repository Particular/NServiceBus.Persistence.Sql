namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence.Sql;

    public class When_sagas_cant_be_found : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task IHandleSagaNotFound_only_called_once()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ReceiverWithSagas>(b => b.When((session, c) => session.SendLocal(new MessageToSaga
                {
                    Id = Guid.NewGuid()
                })))
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual(1, context.TimesFired);
        }

        [Test]
        public async Task IHandleSagaNotFound_not_called_if_second_saga_is_executed()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ReceiverWithOrderedSagas>(b => b.When((session, c) => session.SendLocal(new MessageToSaga
                {
                    Id = Guid.NewGuid()
                })))
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual(0, context.TimesFired);
        }

        public class Context : ScenarioContext
        {
            public int TimesFired { get; set; }
            public bool Done { get; set; }
        }

        public class ReceiverWithSagas : EndpointConfigurationBuilder
        {
            public ReceiverWithSagas()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class MessageToSagaHandler : IHandleMessages<MessageToSaga>
            {
                public Task Handle(MessageToSaga message, IMessageHandlerContext context)
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(TimeSpan.FromMilliseconds(1));
                    options.RouteToThisEndpoint();

                    return context.Send(new FinishMessage(), options);
                }
            }

            public class FinishHandler : IHandleMessages<FinishMessage>
            {
                public Context Context { get; set; }

                public Task Handle(FinishMessage message, IMessageHandlerContext context)
                {
                    // This acts as a safe guard to abort the test earlier
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }

            [CorrelatedSaga(correlationProperty: nameof(CantBeFoundSaga1Data.MessageId))]
            public class CantBeFoundSaga1 : SqlSaga<CantBeFoundSaga1.CantBeFoundSaga1Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
            {
                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.MessageId = message.Id;
                    return Task.FromResult(0);
                }

                public Task Handle(MessageToSaga message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureMapping(MessagePropertyMapper<CantBeFoundSaga1Data> mapper)
                {
                    mapper.MapMessage<StartSaga>(m => m.Id);
                    mapper.MapMessage<MessageToSaga>(m => m.Id);
                }

                public class CantBeFoundSaga1Data : ContainSagaData
                {
                    public virtual Guid MessageId { get; set; }
                }
            }

            [CorrelatedSaga(correlationProperty: nameof(CantBeFoundSaga2Data.MessageId))]
            public class CantBeFoundSaga2 : SqlSaga<CantBeFoundSaga2.CantBeFoundSaga2Data>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
            {
                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.MessageId = message.Id;
                    return Task.FromResult(0);
                }

                public Task Handle(MessageToSaga message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureMapping(MessagePropertyMapper<CantBeFoundSaga2Data> mapper)
                {
                    mapper.MapMessage<StartSaga>(m => m.Id);
                    mapper.MapMessage<MessageToSaga>(m => m.Id);
                }

                public class CantBeFoundSaga2Data : ContainSagaData
                {
                    public virtual Guid MessageId { get; set; }
                }
            }

            public class SagaNotFound : IHandleSagaNotFound
            {
                public Context TestContext { get; set; }

                public Task Handle(object message, IMessageProcessingContext context)
                {
                    TestContext.TimesFired++;
                    TestContext.Done = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class ReceiverWithOrderedSagas : EndpointConfigurationBuilder
        {
            public ReceiverWithOrderedSagas()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<TimeoutManager>();
                    c.ExecuteTheseHandlersFirst(typeof(Saga1), typeof(Saga2));
                });
            }

            public class MessageToSagaHandler : IHandleMessages<MessageToSaga>
            {
                public Task Handle(MessageToSaga message, IMessageHandlerContext context)
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(TimeSpan.FromMilliseconds(1));
                    options.RouteToThisEndpoint();

                    return context.Send(new FinishMessage(), options);
                }
            }

            public class FinishHandler : IHandleMessages<FinishMessage>
            {
                public Context Context { get; set; }

                public Task Handle(FinishMessage message, IMessageHandlerContext context)
                {
                    // This acts as a safe guard to abort the test earlier
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }

            [CorrelatedSaga(correlationProperty: nameof(SagaData.MessageId))]
            public class Saga1 : SqlSaga<Saga1.SagaData>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
            {
                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.MessageId = message.Id;
                    return Task.FromResult(0);
                }

                public Task Handle(MessageToSaga message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
                {
                    mapper.MapMessage<StartSaga>(m => m.Id);
                    mapper.MapMessage<MessageToSaga>(m => m.Id);
                }

                public class SagaData : ContainSagaData
                {
                    public virtual Guid MessageId { get; set; }
                }
            }

            [CorrelatedSaga(correlationProperty: nameof(SagaData.MessageId))]
            public class Saga2 : SqlSaga<Saga2.SagaData>, IHandleMessages<StartSaga>, IAmStartedByMessages<MessageToSaga>
            {
                public Context Context { get; set; }

                public Task Handle(MessageToSaga message, IMessageHandlerContext context)
                {
                    Data.MessageId = message.Id;
                    Context.Done = true;
                    return Task.FromResult(0);
                }

                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.MessageId = message.Id;
                    return Task.FromResult(0);
                }

                protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
                {
                    mapper.MapMessage<StartSaga>(m => m.Id);
                    mapper.MapMessage<MessageToSaga>(m => m.Id);
                }

                public class SagaData : ContainSagaData
                {
                    public virtual Guid MessageId { get; set; }
                }
            }

            public class SagaNotFound : IHandleSagaNotFound
            {
                public Context TestContext { get; set; }

                public Task Handle(object message, IMessageProcessingContext context)
                {
                    TestContext.TimesFired++;
                    return Task.FromResult(0);
                }
            }
        }

        
        public class StartSaga : ICommand
        {
            public Guid Id { get; set; }
        }

        
        public class FinishMessage : ICommand
        {
        }

        
        public class MessageToSaga : ICommand
        {
            public Guid Id { get; set; }
        }
    }
}