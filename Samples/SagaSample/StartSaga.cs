using NServiceBus;

public class StartSaga:IMessage
{
    public string MySagaId { get; set; }
}