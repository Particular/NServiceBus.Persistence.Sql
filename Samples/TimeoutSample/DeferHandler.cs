using NServiceBus;
using NServiceBus.Logging;

public class DeferHandler : IHandleMessages<DeferMessage>
{

    static ILog logger = LogManager.GetLogger(typeof(DeferHandler));
    public void Handle(DeferMessage message)
    {
        logger.Info("Received DeferMessage  "+ message.Property);
    }
}