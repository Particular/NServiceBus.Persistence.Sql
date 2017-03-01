using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Persistence.Sql;

[SqlSaga(
    correlationProperty: nameof(SagaData.OrderId)
)]
public class OrderSaga :
    SqlSaga<OrderSaga.SagaData>,
    IAmStartedByMessages<StartOrder>
{
    static ILog log = LogManager.GetLogger<OrderSaga>();

    protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
    {
    }

    public Task Handle(StartOrder message, IMessageHandlerContext context)
    {
        return Task.FromResult(0);
    }
    
    public class SagaData :
        ContainSagaData
    {
        public Guid OrderId { get; set; }
    }
}