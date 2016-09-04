using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Persistence.Sql.Xml;

public class MySaga : XmlSaga<MySaga.SagaData>,
    IAmStartedByMessages<StartSagaMessage>,
    IHandleMessages<CompleteSagaMessage>
{
    static ILog logger = LogManager.GetLogger(typeof(MySaga));

    protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
    {
        mapper.MapMessage<StartSagaMessage>(m => m.MySagaId);
        mapper.MapMessage<CompleteSagaMessage>(m => m.MySagaId);
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
        MarkAsComplete();
        return Task.FromResult(0);
    }

    public class SagaData : ContainSagaData
    {
        [CorrelationId]
        public Guid MySagaId { get; set; }
    }

}
