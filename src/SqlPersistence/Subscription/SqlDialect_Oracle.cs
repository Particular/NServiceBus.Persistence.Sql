namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Unicast.Subscriptions;

    public partial class SqlDialect
    {
        public partial class Oracle
        {
            internal override string GetSubscriptionTableName(string tablePrefix)
            {
                return $"{SchemaPrefix}\"{tablePrefix.ToUpper()}SS\"";
            }

            internal override string GetSubscriptionSubscribeCommand(string tableName)
            {
                return $@"
begin
    insert into {tableName}
    (
        MessageType,
        Subscriber,
        Endpoint,
        PersistenceVersion
    )
    values
    (
        :MessageType,
        :Subscriber,
        :Endpoint,
        :PersistenceVersion
    );
    commit;
exception
    when DUP_VAL_ON_INDEX then
    if :Endpoint is not null then
        update {tableName} set
            Endpoint = :Endpoint,
            PersistenceVersion = :PersistenceVersion
        where 
            MessageType = :MessageType
            and Subscriber = :Subscriber;
    else
        ROLLBACK;
    end if;
end;
";
            }

            internal override string GetSubscriptionUnsubscribeCommand(string tableName)
            {
                return $@"
delete from {tableName}
where
    Subscriber = :Subscriber and
    MessageType = :MessageType";
            }

            internal override Func<List<MessageType>, string> GetSubscriptionQueryFactory(string tableName)
            {
                var getSubscribersPrefixOracle = $@"
select distinct Subscriber, Endpoint
from {tableName}
where MessageType in (";

                return messageTypes =>
                {
                    var builder = new StringBuilder(getSubscribersPrefixOracle);
                    for (var i = 0; i < messageTypes.Count; i++)
                    {
                        var paramName = $":type{i}";
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