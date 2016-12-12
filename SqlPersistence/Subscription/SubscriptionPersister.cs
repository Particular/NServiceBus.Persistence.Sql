using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

class SubscriptionPersister : ISubscriptionStorage
{
    Func<Task<DbConnection>> connectionBuilder;
    string tablePrefix;
    string subscribeCommandText;
    string unsubscribeCommandText;

    public SubscriptionPersister(
        Func<Task<DbConnection>> connectionBuilder, 
        string tablePrefix)
    {
        this.connectionBuilder = connectionBuilder;
        this.tablePrefix = tablePrefix;

        var tableName = $@"{tablePrefix}SubscriptionData";
        subscribeCommandText = $@"
declare @dummy int; MERGE {tableName} WITH (HOLDLOCK) AS target
USING(SELECT @Endpoint AS Endpoint, @Subscriber AS Subscriber, @MessageType AS MessageType) AS source
ON target.Endpoint = source.Endpoint and 
   target.Subscriber = source.Subscriber and
   target.MessageType = source.MessageType
when matched then
    UPDATE set @dummy = 0
when not matched then
insert
(
    Endpoint,
    Subscriber,
    MessageType,
    PersistenceVersion
)
values
(
    @Endpoint,
    @Subscriber,
    @MessageType,
    @PersistenceVersion
);";

        unsubscribeCommandText = $@"
delete from {tableName}
where
    Subscriber = @Subscriber and
    MessageType = @MessageType";
    }


    public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
    {
        using (var connection = await connectionBuilder())
        {
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
        using (var connection = await connectionBuilder())
        {
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
SELECT DISTINCT Subscriber, Endpoint
from {tablePrefix}SubscriptionData
where MessageType IN (");

        var types = messageTypes.ToList();

        using (var connection = await connectionBuilder())
        using (var command = connection.CreateCommand())
        {
            for (var i = 0; i < types.Count; i++)
            {
                var messageType = types[i];
                var paramName = "@type" + i;
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
                    var endpoint = await reader.IsDBNullAsync(1) ? null : reader.GetString(1);
                    subscribers.Add(new Subscriber(address, endpoint));
                }
                return subscribers;
            }
        }
    }
}