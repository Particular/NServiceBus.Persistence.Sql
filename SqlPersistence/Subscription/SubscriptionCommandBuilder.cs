using System;
using System.Text;

namespace NServiceBus.Persistence.Sql
{
    public static class SubscriptionCommandBuilder
    {

        public static SubscriptionCommands Build(SqlVariant sqlVariant, string tablePrefix)
        {
            var tableName = $@"{tablePrefix}SubscriptionData";

            string subscribeCommandText;
            switch (sqlVariant)
            {
                case SqlVariant.MsSqlServer:
                    subscribeCommandText = $@"
declare @dummy int;
merge {tableName} with (holdlock) as target
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
                    break;

                case SqlVariant.MySql:
                    subscribeCommandText = $@"
insert into {tableName}
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
)
on duplicate key update
    Endpoint = @Endpoint,
    PersistenceVersion = @PersistenceVersion
";
                    break;

                default:
                    throw new Exception($"Unknown SqlVariant: {sqlVariant}.");
            }

            string unsubscribeCommandText = $@"
delete from {tableName}
where
    Subscriber = @Subscriber and
    MessageType = @MessageType";

            var getSubscribersPrefix = $@"
select distinct Subscriber, Endpoint
from {tablePrefix}SubscriptionData
where MessageType in (";

            return new SubscriptionCommands(
                subscribe: subscribeCommandText,
                unsubscribe: unsubscribeCommandText, 
                getSubscribers: messageTypes =>
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
                });
        }

    }
}