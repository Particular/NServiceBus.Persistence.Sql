using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

static class SubscriptionPersisterExtensions
{
    public static Task<IEnumerable<Subscriber>> GetSubscribers(this SubscriptionPersister persister, MessageType messageHierarchy, CancellationToken cancellationToken = default) =>
        GetSubscribers(persister, new[] { messageHierarchy }, cancellationToken);

    public static Task<IEnumerable<Subscriber>> GetSubscribers(this SubscriptionPersister persister, MessageType[] messageHierarchy, CancellationToken cancellationToken = default) =>
        persister.GetSubscriberAddressesForMessage(messageHierarchy, null, cancellationToken);
}
