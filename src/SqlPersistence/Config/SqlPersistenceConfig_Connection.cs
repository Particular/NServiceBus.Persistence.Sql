namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using Configuration.AdvancedExtensibility;
    using Persistence.Sql;
    using Settings;
    using Transport;

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Configures how <see cref="DbConnection"/>s are constructed.
        /// </summary>
        public static void ConnectionBuilder(this PersistenceExtensions<SqlPersistence> configuration, Func<DbConnection> connectionBuilder)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(connectionBuilder), connectionBuilder);

            configuration.GetSettings()
                .Set("SqlPersistence.ConnectionManager", new SingleTenantConnectionManager(connectionBuilder));
        }

        /// <summary>
        /// Configures how <see cref="DbConnection"/>s are constructed, allowing for selecting a different database per tenant in a multi-tenant system.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="tenantIdHeaderName">The name of the message header that identifies the tenant id in each message.</param>
        /// <param name="buildConnectionFromTenantData">Using a tenant id, builds a database connection for that tenant database.</param>
        public static void MultiTenantConnectionBuilder(this PersistenceExtensions<SqlPersistence> configuration, string tenantIdHeaderName, Func<string, DbConnection> buildConnectionFromTenantData)
        {
            var captureTenantId = new Func<IncomingMessage, string>(incomingMessage =>
            {
                if (incomingMessage.Headers.TryGetValue(tenantIdHeaderName, out var tenantId))
                {
                    return tenantId;
                }

                return null;
            });

            MultiTenantConnectionBuilder(configuration, captureTenantId, buildConnectionFromTenantData);
        }

        /// <summary>
        /// Configures how <see cref="DbConnection"/>s are constructed, allowing for selecting a different database per tenant in a multi-tenant system.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="captureTenantId">Determines the the TenantId based on the incoming message, with the ability to consider multiple message headers or transition from one header to another.</param>
        /// <param name="buildConnectionFromTenantData">Using a tenant id, builds a database connection for that tenant database.</param>
        public static void MultiTenantConnectionBuilder(this PersistenceExtensions<SqlPersistence> configuration, Func<IncomingMessage, string> captureTenantId, Func<string, DbConnection> buildConnectionFromTenantData)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(captureTenantId), captureTenantId);
            Guard.AgainstNull(nameof(buildConnectionFromTenantData), buildConnectionFromTenantData);

            var connectionManager = new MultiTenantConnectionManager(captureTenantId, buildConnectionFromTenantData);

            var settings = configuration.GetSettings();
            settings.Set($"SqlPersistence.ConnectionManager.{typeof(StorageType.Outbox).Name}", connectionManager);
            settings.Set($"SqlPersistence.ConnectionManager.{typeof(StorageType.Sagas).Name}", connectionManager);
            settings.Set("SqlPersistence.MultiTenant", true);
        }

        internal static ConnectionManager GetConnectionBuilder(this ReadOnlySettings settings, Type storageType)
        {
            if (settings.TryGet($"SqlPersistence.ConnectionManager.{storageType.Name}", out ConnectionManager value))
            {
                return value;
            }
            if (settings.TryGet("SqlPersistence.ConnectionManager", out value))
            {
                return value;
            }

            var exceptionMessage = $"Couldn't find connection string for {storageType.Name}. The connection to the database must be specified using the `{nameof(ConnectionBuilder)}` method.";

            if (settings.EndpointIsMultiTenant())
            {
                exceptionMessage += $" When in multi-tenant mode with `{nameof(MultiTenantConnectionBuilder)}`, you must still use `{nameof(ConnectionBuilder)}` to provide a database connection for subscriptions/timeouts on message transports that don't support those features natively.";
            }

            throw new Exception(exceptionMessage);
        }

        internal static ConnectionManager GetConnectionBuilder<T>(this ReadOnlySettings settings)
            where T : StorageType
        {
            return GetConnectionBuilder(settings, typeof(T));
        }

    }
}