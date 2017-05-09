using System;
using System.Collections.Generic;
using NServiceBus.Unicast.Subscriptions;
#pragma warning disable 1591

namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// Not for public use.
    /// </summary>
    [Obsolete("Not for public use")]
    public class SubscriptionCommands
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
}