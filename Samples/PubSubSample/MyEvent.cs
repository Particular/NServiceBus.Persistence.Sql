using NServiceBus;

public class MyEvent:IEvent
{
    public string Property { get; set; }
}