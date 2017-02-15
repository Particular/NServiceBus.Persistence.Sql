using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

class SubscriptionPersister : ISubscriptionStorage
{
    Func<DbConnection> connectionBuilder;
    SubscriptionCommands subscriptionCommands;

    public SubscriptionPersister(Func<DbConnection> connectionBuilder, string tablePrefix, SqlVariant sqlVariant)
    {
        this.connectionBuilder = connectionBuilder;
        subscriptionCommands = SubscriptionCommandBuilder.Build(sqlVariant, tablePrefix);
    }


    public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
    {
        using (var connection = await connectionBuilder.OpenConnection())
        {
            await Subscribe(subscriber, connection, messageType);
        }
    }

    async Task Subscribe(Subscriber subscriber, DbConnection connection, MessageType messageType)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = subscriptionCommands.Subscribe;
            command.AddParameter("MessageType", messageType.TypeName);
            command.AddParameter("Subscriber", subscriber.TransportAddress);
            command.AddParameter("Endpoint", subscriber.Endpoint);
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            await command.ExecuteNonQueryEx();
        }
    }


    public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
    {
        using (var connection = await connectionBuilder.OpenConnection())
        {
            await Unsubscribe(subscriber, connection, messageType);
        }
    }

    async Task Unsubscribe(Subscriber subscriber, DbConnection connection, MessageType messageType)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = subscriptionCommands.Unsubscribe;
            command.AddParameter("MessageType", messageType.TypeName);
            command.AddParameter("Subscriber", subscriber.TransportAddress);
            await command.ExecuteNonQueryEx();
        }
    }

    public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
    {
        var types = messageTypes.ToList();

        var getSubscribersCommand = subscriptionCommands.GetSubscribers(types);
        using (var connection = await connectionBuilder.OpenConnection())
        using (var command = connection.CreateCommand())
        {
            for (var i = 0; i < types.Count; i++)
            {
                var messageType = types[i];
                var paramName = $"@type{i}";
                command.AddParameter(paramName, messageType.TypeName);
            }
            command.CommandText = getSubscribersCommand;
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