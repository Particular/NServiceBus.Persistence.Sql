using NServiceBus;

public class StartSagaMessage:IMessage
{
    public string MySagaId { get; set; }
}