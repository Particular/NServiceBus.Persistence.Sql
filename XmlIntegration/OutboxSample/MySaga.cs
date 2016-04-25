using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Persistence.SqlServerXml;

public class MySaga2 : XmlSaga<MySaga2.SagaData>,
    IAmStartedByMessages<StartSagaMessage>,
    IHandleMessages<CompleteSagaMessage>
{
    static ILog logger = LogManager.GetLogger(typeof(MySaga2));

    protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
    {
        mapper.MapMessage<StartSagaMessage>(m => m.MySagaId);
        mapper.MapMessage<CompleteSagaMessage>(m => m.MySagaId);
    }

    public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
    {
        Data.MySagaId = message.MySagaId;
        logger.Info($"Start Saga. Data.MySagaId:{Data.MySagaId}. Message.MySagaId:{message.MySagaId}");
        return context.SendLocal(new CompleteSagaMessage
                           {
                               MySagaId = Data.MySagaId
                           });
    }

    public Task Handle(CompleteSagaMessage message, IMessageHandlerContext context)
    {
        logger.Info($"Completed Saga. Data.MySagaId:{Data.MySagaId}. Message.MySagaId:{message.MySagaId}");
        MarkAsComplete();
        return Task.FromResult(0);
    }

    public class SagaData : XmlSagaData
    {
        [CorrelationId]
        public Guid MySagaId { get; set; }
    }

}
