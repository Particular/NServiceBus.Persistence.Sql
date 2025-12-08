using System;
using System.Threading.Tasks;
using NServiceBus;

public class OrderSaga : Saga<OrderSaga.SagaData>,
    IAmStartedByMessages<StartOrder>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper) => mapper.MapSaga(s => s.OrderId).ToMessage<StartOrder>(m => m.OrderId);

    public Task Handle(StartOrder message, IMessageHandlerContext context) => Task.CompletedTask;

    public class SagaData : ContainSagaData
    {
        public Guid OrderId { get; set; }
    }
}