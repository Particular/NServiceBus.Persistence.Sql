using NServiceBus;
using NServiceBus.Logging;

public class MyEventHandler : IHandleMessages<MyEvent>
{

    static ILog logger = LogManager.GetLogger(typeof(MyEventHandler));
    public void Handle(MyEvent message)
    {
        logger.Info("Received MyEvent  " + message.Property);
    }
}