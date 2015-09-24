using NServiceBus;

public class SagaTimout : IMessage
{
    public string Property { get; set; }
}