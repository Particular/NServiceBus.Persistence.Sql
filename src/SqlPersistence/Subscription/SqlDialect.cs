namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Unicast.Subscriptions;

    public partial class SqlDialect
    {
        internal abstract string GetSubscriptionTableName(string tablePrefix);

        internal abstract string GetSubscriptionSubscribeCommand(string tableName);
        internal abstract string GetSubscriptionUnsubscribeCommand(string tableName);
        internal abstract Func<List<MessageType>, string> GetSubscriptionQueryFactory(string tableName);
    }
}