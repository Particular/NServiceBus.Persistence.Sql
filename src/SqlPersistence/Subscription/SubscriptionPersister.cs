using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Persistence.Sql;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

#pragma warning disable 618

class SubscriptionPersister : ISubscriptionStorage
{
    public SubscriptionPersister(IConnectionManager connectionManager, string tablePrefix, SqlDialect sqlDialect, TimeSpan? cacheFor)
    {
        this.connectionManager = connectionManager;
        this.sqlDialect = sqlDialect;
        this.cacheFor = cacheFor;
        subscriptionCommands = SubscriptionCommandBuilder.Build(sqlDialect, tablePrefix);
        if (cacheFor != null)
        {
            Cache = new ConcurrentDictionary<string, CacheItem>();
        }
    }

    public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
    {
        await Retry(async () =>
        {
            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            using (var connection = await connectionManager.OpenNonContextualConnection().ConfigureAwait(false))
            using (var command = sqlDialect.CreateCommand(connection))
            {
                command.CommandText = subscriptionCommands.Subscribe;
                command.AddParameter("MessageType", messageType.TypeName);
                command.AddParameter("Subscriber", subscriber.TransportAddress);
                command.AddParameter("Endpoint", Nullable(subscriber.Endpoint));
                command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
                await command.ExecuteNonQueryEx().ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
        ClearForMessageType(messageType);
    }

    public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
    {
        await Retry(async () =>
        {
            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            using (var connection = await connectionManager.OpenNonContextualConnection().ConfigureAwait(false))
            using (var command = sqlDialect.CreateCommand(connection))
            {
                command.CommandText = subscriptionCommands.Unsubscribe;
                command.AddParameter("MessageType", messageType.TypeName);
                command.AddParameter("Subscriber", subscriber.TransportAddress);
                await command.ExecuteNonQueryEx().ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
        ClearForMessageType(messageType);
    }

    public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageHierarchy, ContextBag context)
    {
        var types = messageHierarchy.ToList();

        if (cacheFor == null)
        {
            return GetSubscriptions(types);
        }

        var key = GetKey(types);

        var cacheItem = Cache.GetOrAdd(key,
            valueFactory: _ => new CacheItem
            {
                Stored = DateTime.UtcNow,
                Subscribers = GetSubscriptions(types)
            });

        var age = DateTime.UtcNow - cacheItem.Stored;
        if (age >= cacheFor)
        {
            cacheItem.Subscribers = GetSubscriptions(types);
            cacheItem.Stored = DateTime.UtcNow;
        }
        return cacheItem.Subscribers;
    }

    static object Nullable(object value)
    {
        return value ?? DBNull.Value;
    }

    static async Task Retry(Func<Task> action)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                await action().ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                attempts++;

                if (attempts > 10)
                {
                    throw;
                }
                Log.Debug("Error while processing subscription change request. Retrying.", ex);
                await Task.Delay(100).ConfigureAwait(false);
            }
        }
    }

    void ClearForMessageType(MessageType messageType)
    {
        if (cacheFor == null)
        {
            return;
        }
        var keyPart = GetKeyPart(messageType);
        foreach (var cacheKey in Cache.Keys)
        {
            if (cacheKey.Contains(keyPart))
            {
                Cache.TryRemove(cacheKey, out _);
            }
        }
    }

    static string GetKey(List<MessageType> types)
    {
        var typeNames = types.Select(_ => _.TypeName);
        return string.Join(",", typeNames) + ",";
    }

    static string GetKeyPart(MessageType type)
    {
        return $"{type.TypeName},";
    }

    async Task<IEnumerable<Subscriber>> GetSubscriptions(List<MessageType> messageHierarchy)
    {
        var getSubscribersCommand = subscriptionCommands.GetSubscribers(messageHierarchy);
        using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
        using (var connection = await connectionManager.OpenNonContextualConnection().ConfigureAwait(false))
        using (var command = sqlDialect.CreateCommand(connection))
        {
            for (var i = 0; i < messageHierarchy.Count; i++)
            {
                var messageType = messageHierarchy[i];
                var paramName = $"type{i}";
                command.AddParameter(paramName, messageType.TypeName);
            }
            command.CommandText = getSubscribersCommand;
            using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
            {
                var subscribers = new List<Subscriber>();
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var address = reader.GetString(0);
                    string endpoint;
                    if (await reader.IsDBNullAsync(1).ConfigureAwait(false))
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

    public ConcurrentDictionary<string, CacheItem> Cache;
    IConnectionManager connectionManager;
    SqlDialect sqlDialect;
    TimeSpan? cacheFor;
    SubscriptionCommands subscriptionCommands;
    static ILog Log = LogManager.GetLogger<SubscriptionPersister>();

    internal class CacheItem
    {
        public DateTime Stored;
        public Task<IEnumerable<Subscriber>> Subscribers;
    }
}