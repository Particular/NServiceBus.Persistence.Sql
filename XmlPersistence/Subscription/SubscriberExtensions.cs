using NServiceBus.Routing;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

internal static class SubscriberExtensions
{

    internal static Subscriber ToSubscriber(this string s)
    {
        var split = s.Split('@');
        return new Subscriber(split[0], split[1]);
    }

    internal static string ToAddress(this Subscriber subscriber)
    {
        return $"{subscriber.TransportAddress}@{subscriber.Endpoint}";
    }
}