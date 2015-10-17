using NServiceBus;

public class SagaTimoutMessage : IMessage
{
    public string Property { get; set; }
}