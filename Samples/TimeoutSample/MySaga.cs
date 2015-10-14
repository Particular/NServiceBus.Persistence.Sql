using System;
using NServiceBus.Logging;
using NServiceBus.Saga;
using NServiceBus.SqlPersistence;

public class MySaga : XmlSaga<MySaga.SagaData>,
    IAmStartedByMessages<StartSaga>,
    IHandleTimeouts<SagaTimout>
{
    static ILog logger = LogManager.GetLogger(typeof (MySaga));

    public void Handle(StartSaga message)
    {
        var timout = new SagaTimout
        {
            Property = "PropertyValue"
        };
        RequestTimeout(TimeSpan.FromSeconds(3), timout);
    }


    public void Timeout(SagaTimout state)
    {
        logger.Info("Timeout " + state.Property);
        MarkAsComplete();
    }

    public class SagaData : XmlSagaData
    {
    }
}