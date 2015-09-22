using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

class SubscriptionPersister : ISubscriptionStorage
{
    public void Subscribe(Address client, IEnumerable<MessageType> messageTypes)
    {
        throw new NotImplementedException();
    }

    public void Unsubscribe(Address client, IEnumerable<MessageType> messageTypes)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Address> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
    {
        throw new NotImplementedException();
    }

    public void Init()
    {
        throw new NotImplementedException();
    }
}