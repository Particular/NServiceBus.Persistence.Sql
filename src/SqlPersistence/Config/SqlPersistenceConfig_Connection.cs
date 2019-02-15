namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using Configuration.AdvancedExtensibility;
    using Persistence.Sql;
    using Settings;

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
                .Set("SqlPersistence.ConnectionManager", ConnectionManager.BuildSingleTenant(connectionBuilder));
        }

        /// <summary>
        /// Configures how <see cref="DbConnection"/>s are constructed, allowing for selecting a different database per tenant in a multi-tenant system.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="captureTenantId"></param>
        /// <param name="buildConnectionFromTenantData"></param>
        public static void MultiTenantConnectionBuilder(this PersistenceExtensions<SqlPersistence> configuration, Func<IReadOnlyDictionary<string, string>, string> captureTenantId, Func<string, DbConnection> buildConnectionFromTenantData)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(captureTenantId), captureTenantId);
            Guard.AgainstNull(nameof(buildConnectionFromTenantData), buildConnectionFromTenantData);

            var connectionBuilder = new ConnectionManager(captureTenantId, buildConnectionFromTenantData);

            configuration.GetSettings()
                .Set("SqlPersistence.ConnectionManager", connectionBuilder);
        }

        /// <summary>
        /// Configures how <see cref="DbConnection"/>s are constructed, allowing for selecting a different database per tenant in a multi-tenant system.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="tenantIdHeaderName"></param>
        /// <param name="buildConnectionFromTenantData"></param>
        public static void MultiTenantConnectionBuilder(this PersistenceExtensions<SqlPersistence> configuration, string tenantIdHeaderName, Func<string, DbConnection> buildConnectionFromTenantData)
        {
            var captureTenantId = new Func<IReadOnlyDictionary<string, string>, string>(messageHeaders =>
            {
                if (messageHeaders.TryGetValue(tenantIdHeaderName, out var tenantId))
                {
                    return tenantId;
                }

                return null;
            });

            MultiTenantConnectionBuilder(configuration, captureTenantId, buildConnectionFromTenantData);
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
            throw new Exception($"Couldn't find connection string for {storageType}. The connection to the database must be specified using the ConnectionBuilder method.");
        }

        // TODO: RENAME to GetConnectionManager
        internal static ConnectionManager GetConnectionBuilder<T>(this ReadOnlySettings settings)
            where T : StorageType
        {
            return GetConnectionBuilder(settings, typeof(T));
        }

    }
}