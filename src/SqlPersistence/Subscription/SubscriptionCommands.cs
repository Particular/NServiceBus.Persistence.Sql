using System;
using System.Collections.Generic;
using NServiceBus.Unicast.Subscriptions;

// used by docs engine to create scripts
class SubscriptionCommands
{
    public string Subscribe { get; }
    public string Unsubscribe { get; }
    public Func<List<MessageType>, string> GetSubscribers { get; }

    public SubscriptionCommands(string subscribe, string unsubscribe, Func<List<MessageType>, string> getSubscribers)
    {
        Subscribe = subscribe;
        Unsubscribe = unsubscribe;
        GetSubscribers = getSubscribers;
    }
}