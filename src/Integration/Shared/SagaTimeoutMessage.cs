using NServiceBus;

public class SagaTimeoutMessage : IMessage
{
    public string Property { get; set; }
}