using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Persistence.Sql;

[SqlSaga(
     correlationProperty: nameof(SagaData.MySagaId)
 )]
public class MySaga : Saga<MySaga.SagaData>,
    IAmStartedByMessages<StartSagaMessage>,
    IHandleTimeouts<SagaTimoutMessage>
{
    static ILog logger = LogManager.GetLogger(typeof(MySaga));

    public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
    {
        Data.MySagaId = message.MySagaId;
        var timeout = new SagaTimoutMessage
        {
            Property = "PropertyValue"
        };
        return RequestTimeout(context, TimeSpan.FromSeconds(3), timeout);
    }

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
    {
        mapper.ConfigureMapping<StartSagaMessage>(message => message.MySagaId)
            .ToSaga(data => data.MySagaId);
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