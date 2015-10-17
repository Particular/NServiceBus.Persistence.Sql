using System;
using NServiceBus.Logging;
using NServiceBus.Saga;
using NServiceBus.SqlPersistence;

public class MySaga : XmlSaga<MySaga.SagaData>,
    IAmStartedByMessages<StartSagaMessage>,
    IHandleTimeouts<SagaTimoutMessage>
{
    static ILog logger = LogManager.GetLogger(typeof (MySaga));

    public void Handle(StartSagaMessage message)
    {
        var timout = new SagaTimoutMessage
        {
            Property = "PropertyValue"
        };
        RequestTimeout(TimeSpan.FromSeconds(3), timout);
    }


    public void Timeout(SagaTimoutMessage state)
    {
        logger.Info("Timeout " + state.Property);
        MarkAsComplete();
    }

    public class SagaData : XmlSagaData
    {
    }
}