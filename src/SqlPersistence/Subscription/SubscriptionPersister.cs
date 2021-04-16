using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

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

    public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
    {
        await Retry(
            async token =>
            {
                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                using (var connection = await connectionManager.OpenNonContextualConnection(token).ConfigureAwait(false))
                using (var command = sqlDialect.CreateCommand(connection))
                {
                    command.CommandText = subscriptionCommands.Subscribe;
                    command.AddParameter("MessageType", messageType.TypeName);
                    command.AddParameter("Subscriber", subscriber.TransportAddress);
                    command.AddParameter("Endpoint", Nullable(subscriber.Endpoint));
                    command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
                    _ = await command.ExecuteNonQueryEx(token).ConfigureAwait(false);
                }
            },
            cancellationToken).ConfigureAwait(false);

        ClearForMessageType(messageType);
    }

    public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
    {
        await Retry(
            async token =>
            {
                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                using (var connection = await connectionManager.OpenNonContextualConnection(token).ConfigureAwait(false))
                using (var command = sqlDialect.CreateCommand(connection))
                {
                    command.CommandText = subscriptionCommands.Unsubscribe;
                    command.AddParameter("MessageType", messageType.TypeName);
                    command.AddParameter("Subscriber", subscriber.TransportAddress);
                    _ = await command.ExecuteNonQueryEx(token).ConfigureAwait(false);
                }
            },
            cancellationToken).ConfigureAwait(false);

        ClearForMessageType(messageType);
    }

    public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageHierarchy, ContextBag context, CancellationToken cancellationToken = default)
    {
        var types = messageHierarchy.ToList();

        if (cacheFor == null)
        {
            return GetSubscriptions(types, cancellationToken);
        }

        var key = GetKey(types);

        var cacheItem = Cache.GetOrAdd(key,
            valueFactory: _ => new CacheItem
            {
                Stored = DateTime.UtcNow,
                Subscribers = GetSubscriptions(types, cancellationToken)
            });

        var age = DateTime.UtcNow - cacheItem.Stored;

        if (age >= cacheFor)
        {
            cacheItem.Subscribers = GetSubscriptions(types, cancellationToken);
            cacheItem.Stored = DateTime.UtcNow;
        }

        return cacheItem.Subscribers;
    }

    static object Nullable(object value)
    {
        return value ?? DBNull.Value;
    }

    static async Task Retry(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        var attempts = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await action(cancellationToken).ConfigureAwait(false);
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

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
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

    async Task<IEnumerable<Subscriber>> GetSubscriptions(List<MessageType> messageHierarchy, CancellationToken cancellationToken)
    {
        var getSubscribersCommand = subscriptionCommands.GetSubscribers(messageHierarchy);
        using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
        using (var connection = await connectionManager.OpenNonContextualConnection(cancellationToken).ConfigureAwait(false))
        using (var command = sqlDialect.CreateCommand(connection))
        {
            for (var i = 0; i < messageHierarchy.Count; i++)
            {
                var messageType = messageHierarchy[i];
                var paramName = $"type{i}";
                command.AddParameter(paramName, messageType.TypeName);
            }

            command.CommandText = getSubscribersCommand;
            using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                var subscribers = new List<Subscriber>();
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var address = reader.GetString(0);
                    string endpoint;
                    if (await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false))
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
