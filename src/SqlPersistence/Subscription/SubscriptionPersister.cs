using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
#pragma warning disable 618

class SubscriptionPersister : ISubscriptionStorage
{
    Func<DbConnection> connectionBuilder;
    TimeSpan? cacheFor;
    SubscriptionCommands subscriptionCommands;
    ConcurrentDictionary<string, CacheItem> cache = new ConcurrentDictionary<string, CacheItem>();


    public SubscriptionPersister(Func<DbConnection> connectionBuilder, string tablePrefix, SqlVariant sqlVariant, string schema, TimeSpan? cacheFor)
    {
        this.connectionBuilder = connectionBuilder;
        this.cacheFor = cacheFor;
        subscriptionCommands = SubscriptionCommandBuilder.Build(sqlVariant, tablePrefix, schema);
    }


    public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
    {
        using (var connection = await connectionBuilder.OpenConnection())
        {
            await Subscribe(subscriber, connection, messageType);
        }
        cache.Clear();
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
        cache.Clear();
    }


    public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
    {
        using (var connection = await connectionBuilder.OpenConnection())
        {
            await Unsubscribe(subscriber, connection, messageType);
        }
        cache.Clear();
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

        if (cacheFor == null)
        {
            return await GetSubscriptions(types).ConfigureAwait(false);
        }

        var typeNames = types.Select(_ => _.TypeName);
        var key = string.Join(",", typeNames);
        CacheItem cacheItem;
        if (cache.TryGetValue(key, out cacheItem))
        {
            if (DateTimeOffset.UtcNow - cacheItem.Stored < cacheFor)
            {
                return cacheItem.Subscribers;
            }
        }

        var baseSubscribers = await GetSubscriptions(types)
            .ConfigureAwait(false);

        cacheItem = new CacheItem
        {
            Stored = DateTime.UtcNow,
            Subscribers = baseSubscribers.ToList()
        };

        cache.AddOrUpdate(key, s => cacheItem, (s, tuple) => cacheItem);

        return cacheItem.Subscribers;
    }

    class CacheItem
    {
        public DateTime Stored;
        public List<Subscriber> Subscribers;
    }

    async Task<IEnumerable<Subscriber>> GetSubscriptions(List<MessageType> types)
    {
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