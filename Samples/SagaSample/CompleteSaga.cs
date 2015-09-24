using NServiceBus;

public class CompleteSaga : IMessage
{
    public string MySagaId { get; set; }
}