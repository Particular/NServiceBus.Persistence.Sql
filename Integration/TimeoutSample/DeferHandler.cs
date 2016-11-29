using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class DeferHandler : IHandleMessages<DeferMessage>
{

    static ILog logger = LogManager.GetLogger(typeof(DeferHandler));

    public Task Handle(DeferMessage message, IMessageHandlerContext context)
    {
        logger.Info($"Received DeferMessage {message.Property}");
        return Task.FromResult(0);
    }

}