using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Persistence.SqlServerXml;

public class MySaga : XmlSaga<MySaga.SagaData>,
    IAmStartedByMessages<StartSagaMessage>,
    IHandleTimeouts<SagaTimoutMessage>
{
    static ILog logger = LogManager.GetLogger(typeof (MySaga));

    public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
    {
        var timeout = new SagaTimoutMessage
        {
            Property = "PropertyValue"
        };
        return RequestTimeout(context, TimeSpan.FromSeconds(3), timeout);
    }

    public Task Timeout(SagaTimoutMessage state, IMessageHandlerContext context)
    {
        logger.Info("Timeout " + state.Property);
        MarkAsComplete();
        return Task.FromResult(0);
    }

    public class SagaData : ContainSagaData
    {
    }
}