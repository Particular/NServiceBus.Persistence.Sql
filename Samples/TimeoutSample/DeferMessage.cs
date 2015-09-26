using NServiceBus;

public class DeferMessage:IMessage
{
    public string Property { get; set; }
}