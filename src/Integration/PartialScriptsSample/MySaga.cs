using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class MySaga : Saga<MySaga.SagaData>,
    IAmStartedByMessages<StartSagaMessage>,
    IHandleTimeouts<SagaTimeoutMessage>
{
    static ILog logger = LogManager.GetLogger(typeof(MySaga));

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
    {
        mapper.ConfigureMapping<StartSagaMessage>(message => message.MySagaId)
            .ToSaga(sagaData => sagaData.MySagaId);
    }

    public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
    {
        Data.MySagaId = message.MySagaId;
        logger.Info($"Start Saga. Data.MySagaId:{Data.MySagaId}. Message.MySagaId:{message.MySagaId}");
        var timeout = new SagaTimeoutMessage
        {
            Property = "PropertyValue"
        };
        return RequestTimeout(context, TimeSpan.FromSeconds(3), timeout);
    }

    public Task Timeout(SagaTimeoutMessage state, IMessageHandlerContext context)
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