using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

static class SubscriptionPersisterExtensions
{
    public static Task<IEnumerable<Subscriber>> GetSubscribers(this SubscriptionPersister persister, params MessageType[] messageHierarchy)
    {
        return persister.GetSubscriberAddressesForMessage(messageHierarchy, null);
    }
}