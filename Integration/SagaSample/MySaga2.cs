using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.SqlPersistence;

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

    public async Task Handle(StartSagaMessage message, IMessageHandlerContext context)
    {
        Data.MySagaId = message.MySagaId;
        logger.Info($"Start Saga. Data.MySagaId:{Data.MySagaId}. Message.MySagaId:{message.MySagaId}");
        await context.SendLocalAsync(new CompleteSagaMessage
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
