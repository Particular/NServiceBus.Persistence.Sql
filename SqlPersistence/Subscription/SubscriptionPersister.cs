using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

class SubscriptionPersister : ISubscriptionStorage
{
    Func<DbConnection> connectionBuilder;
    string tablePrefix;
    string subscribeCommandText;
    string unsubscribeCommandText;

    public SubscriptionPersister(
        Func<DbConnection> connectionBuilder,
        string tablePrefix, SqlVarient sqlVarient)
    {
        this.connectionBuilder = connectionBuilder;
        this.tablePrefix = tablePrefix;

        var tableName = $@"{tablePrefix}SubscriptionData";

        switch (sqlVarient)
        {
            case SqlVarient.MsSqlServer:
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

            case SqlVarient.MySql:
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
                throw new Exception($"Unknown SqlVarient: {sqlVarient}.");
        }

        unsubscribeCommandText = $@"
delete from {tableName}
where
    Subscriber = @Subscriber and
    MessageType = @MessageType";
    }


    public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
    {
        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync();
            await Subscribe(subscriber, connection, messageType);
        }
    }

    async Task Subscribe(Subscriber subscriber, DbConnection connection, MessageType messageType)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = subscribeCommandText;
            command.AddParameter("MessageType", messageType.TypeName);
            command.AddParameter("Subscriber", subscriber.TransportAddress);
            command.AddParameter("Endpoint", subscriber.Endpoint);
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            await command.ExecuteNonQueryEx();
        }
    }


    public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
    {
        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync();
            await Unsubscribe(subscriber, connection, messageType);
        }
    }

    async Task Unsubscribe(Subscriber subscriber, DbConnection connection, MessageType messageType)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = unsubscribeCommandText;
            command.AddParameter("MessageType", messageType.TypeName);
            command.AddParameter("Subscriber", subscriber.TransportAddress);
            await command.ExecuteNonQueryEx();
        }
    }

    public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
    {
        var builder = new StringBuilder();

        builder.Append($@"
select distinct Subscriber, Endpoint
from {tablePrefix}SubscriptionData
where MessageType in (");

        var types = messageTypes.ToList();

        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                for (var i = 0; i < types.Count; i++)
                {
                    var messageType = types[i];
                    var paramName = $"@type{i}";
                    builder.Append(paramName);
                    if (i < types.Count - 1)
                    {
                        builder.Append(", ");
                    }
                    command.AddParameter(paramName, messageType.TypeName);
                }
                builder.Append(")");
                command.CommandText = builder.ToString();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var subscribers = new List<Subscriber>();
                    while (await reader.ReadAsync())
                    {
                        var address = reader.GetString(0);
                        string endpoint;
                        if (await reader.IsDBNullAsync(1))
                        {
                            endpoint = null;
                        }
                        else
                        {
                            endpoint = reader.GetString(1);
                        }
                        subscribers.Add(new Subscriber(address, endpoint));
                    }
                    return subscribers;
                }
            }
        }
    }
}