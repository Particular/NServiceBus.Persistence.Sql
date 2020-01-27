namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Collections.Generic;
    using Unicast.Subscriptions;

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
}