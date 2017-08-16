namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Unicast.Subscriptions;

    public partial class SqlDialect
    {
        public partial class MsSqlServer
        {
            internal override string GetSubscriptionTableName(string tablePrefix)
            {
                return $"[{Schema}].[{tablePrefix}SubscriptionData]";
            }

            internal override string GetSubscriptionSubscribeCommand(string tableName)
            {
                return $@"
declare @dummy int;
merge {tableName} with (holdlock, tablock) as target
using(select @Endpoint as Endpoint, @Subscriber as Subscriber, @MessageType as MessageType) as source
on target.Endpoint = source.Endpoint and
   target.Subscriber = source.Subscriber and
   target.MessageType = source.MessageType
when matched then
    update set @dummy = 0
when not matched then
insert
(
    Subscriber,
    MessageType,
    Endpoint,
    PersistenceVersion
)
values
(
    @Subscriber,
    @MessageType,
    @Endpoint,
    @PersistenceVersion
);";
            }

            internal override string GetSubscriptionUnsubscribeCommand(string tableName)
            {
                return $@"
delete from {tableName}
where
    Subscriber = @Subscriber and
    MessageType = @MessageType";
            }

            internal override Func<List<MessageType>, string> GetSubscriptionQueryFactory(string tableName)
            {
                var getSubscribersPrefix = $@"
select distinct Subscriber, Endpoint
from {tableName}
where MessageType in (";

                return messageTypes =>
                {
                    var builder = new StringBuilder(getSubscribersPrefix);
                    for (var i = 0; i < messageTypes.Count; i++)
                    {
                        var paramName = $"@type{i}";
                        builder.Append(paramName);
                        if (i < messageTypes.Count - 1)
                        {
                            builder.Append(", ");
                        }
                    }
                    builder.Append(")");
                    return builder.ToString();
                };
            }
        }
    }
}