using System;
using System.Collections.Generic;
using NServiceBus.Unicast.Subscriptions;

namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// Not for public use.
    /// </summary>
    [Obsolete("Not for public use")]
    public class SubscriptionCommands
    {
        public readonly string Subscribe;
        public readonly string Unsubscribe;
        public readonly Func<List<MessageType>, string> GetSubscribers;

        public SubscriptionCommands(string subscribe, string unsubscribe, Func<List<MessageType>, string> getSubscribers)
        {
            Subscribe = subscribe;
            Unsubscribe = unsubscribe;
            GetSubscribers = getSubscribers;
        }
    }
}