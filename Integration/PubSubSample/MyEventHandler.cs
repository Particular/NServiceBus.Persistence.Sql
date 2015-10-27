using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class MyEventHandler : IHandleMessages<MyEvent>
{

    static ILog logger = LogManager.GetLogger(typeof(MyEventHandler));
    public Task Handle(MyEvent message, IMessageHandlerContext context)
    {
        logger.Info("Received MyEvent  " + message.Property);
        return Task.FromResult(0);
    }

}