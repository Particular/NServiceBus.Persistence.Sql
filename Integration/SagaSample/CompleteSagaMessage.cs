using NServiceBus;

public class CompleteSagaMessage : IMessage
{
    public string MySagaId { get; set; }
}