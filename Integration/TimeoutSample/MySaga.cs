using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.SqlPersistence;

public class MySaga : XmlSaga<MySaga.SagaData>,
    IAmStartedByMessages<StartSagaMessage>,
    IHandleTimeouts<SagaTimoutMessage>
{
    static ILog logger = LogManager.GetLogger(typeof (MySaga));

    public async Task Handle(StartSagaMessage message, IMessageHandlerContext context)
    {
        var timout = new SagaTimoutMessage
        {
            Property = "PropertyValue"
        };
        await RequestTimeoutAsync(context, TimeSpan.FromSeconds(3), timout);
    }


    public Task Timeout(SagaTimoutMessage state, IMessageHandlerContext context)
    {
        logger.Info("Timeout " + state.Property);
        MarkAsComplete();
        return Task.FromResult(0);
    }

    public class SagaData : XmlSagaData
    {
    }
    
    
}