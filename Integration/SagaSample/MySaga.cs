using System;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Saga;
using NServiceBus.SqlPersistence;

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

    public void Handle(StartSagaMessage message)
    {
        Data.MySagaId = message.MySagaId;
        logger.Info($"Start Saga. Data.MySagaId:{Data.MySagaId}. Message.MySagaId:{message.MySagaId}");
        Bus.SendLocal(new CompleteSagaMessage
                           {
                               MySagaId = Data.MySagaId
                           });
    }

    public void Handle(CompleteSagaMessage message)
    {
        logger.Info($"Completed Saga. Data.MySagaId:{Data.MySagaId}. Message.MySagaId:{message.MySagaId}");
        MarkAsComplete();
    }

    public class SagaData : XmlSagaData
    {
        [CorrelationId]
        public Guid MySagaId { get; set; }
    }
}
