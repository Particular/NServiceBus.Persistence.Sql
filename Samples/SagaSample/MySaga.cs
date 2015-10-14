using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Saga;
using NServiceBus.SqlPersistence;

public class MySaga : XmlSaga<MySaga.SagaData>,
    IAmStartedByMessages<StartSaga>,
    IHandleMessages<CompleteSaga>
{
    static ILog logger = LogManager.GetLogger(typeof(MySaga));

    protected override void ConfigureHowToFindSaga(MessagePropertyMapper<SagaData> mapper)
    {
        mapper.MapMessage<StartSaga>(m => m.MySagaId);
        mapper.MapMessage<CompleteSaga>(m => m.MySagaId);
    }

    public void Handle(StartSaga message)
    {
        Data.MySagaId = message.MySagaId;
        logger.Info($"Start Saga. Data.MySagaId:{Data.MySagaId}. Message.MySagaId:{message.MySagaId}");
        Bus.SendLocal(new CompleteSaga
                           {
                               MySagaId = Data.MySagaId
                           });
    }

    public void Handle(CompleteSaga message)
    {
        logger.Info($"Completed Saga. Data.MySagaId:{Data.MySagaId}. Message.MySagaId:{message.MySagaId}");
        MarkAsComplete();
    }

    public class SagaData : XmlSagaData
    {
        public string MySagaId { get; set; }
    }
}
