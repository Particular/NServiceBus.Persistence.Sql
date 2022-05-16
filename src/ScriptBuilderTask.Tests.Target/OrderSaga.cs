using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql;

public class OrderSaga :
    SqlSaga<OrderSaga.SagaData>,
    IAmStartedByMessages<StartOrder>
{
    protected override string CorrelationPropertyName => nameof(SagaData.OrderId);

    protected override void ConfigureMapping(IMessagePropertyMapper mapper)
    {
    }

    public Task Handle(StartOrder message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }

    public class SagaData :
        ContainSagaData
    {
        public Guid OrderId { get; set; }
    }
}