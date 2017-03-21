using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Persistence.Sql;

[SqlSaga(
     CorrelationProperty = nameof(SagaData.MySagaId)
 )]
public class MySaga : SqlSaga<MySaga.SagaData>,
    IAmStartedByMessages<StartSagaMessage>,
    IHandleMessages<CompleteSagaMessage>,
    IHandleTimeouts<SagaTimoutMessage>
{
    static ILog logger = LogManager.GetLogger(typeof(MySaga));

    protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
    {
        mapper.MapMessage<StartSagaMessage>(_ => _.MySagaId);
        mapper.MapMessage<CompleteSagaMessage>(_ => _.MySagaId);
    }

    public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
    {
        Data.MySagaId = message.MySagaId;
        logger.Info($"Start Saga. Data.MySagaId:{Data.MySagaId}. Message.MySagaId:{message.MySagaId}");
        var completeSagaMessage = new CompleteSagaMessage
        {
            MySagaId = Data.MySagaId
        };
        return context.SendLocal(completeSagaMessage);
    }

    public Task Handle(CompleteSagaMessage message, IMessageHandlerContext context)
    {
        logger.Info($"Completed Saga. Data.MySagaId:{Data.MySagaId}. Message.MySagaId:{message.MySagaId}");
        var timeout = new SagaTimoutMessage
        {
            Property = "PropertyValue"
        };
        return RequestTimeout(context, TimeSpan.FromSeconds(1), timeout);
    }

    public Task Timeout(SagaTimoutMessage state, IMessageHandlerContext context)
    {
        logger.Info($"Timeout {state.Property}");
        MarkAsComplete();
        return Task.FromResult(0);
    }


    public class SagaData : ContainSagaData
    {
        public Guid MySagaId { get; set; }
    }

}