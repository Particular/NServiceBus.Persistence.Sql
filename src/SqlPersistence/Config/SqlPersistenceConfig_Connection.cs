namespace NServiceBus
{
    using System;
    using System.Data.Common;
    using Configuration.AdvancedExtensibility;
    using Extensibility;
    using Persistence.Sql;
    using Settings;

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Configures how <see cref="DbConnection"/>s are constructed.
        /// </summary>
        public static void ConnectionBuilder(this PersistenceExtensions<SqlPersistence> configuration, Func<ContextBag, DbConnection> connectionBuilder)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(connectionBuilder), connectionBuilder);
            configuration.GetSettings()
                .Set("SqlPersistence.ConnectionBuilder", connectionBuilder);
        }

        internal static Func<ContextBag, DbConnection> GetConnectionBuilder(this ReadOnlySettings settings, Type storageType)
        {
            if (settings.TryGet($"SqlPersistence.ConnectionBuilder.{storageType.Name}", out Func<ContextBag, DbConnection> value))
            {
                return value;
            }
            if (settings.TryGet("SqlPersistence.ConnectionBuilder", out value))
            {
                return value;
            }
            throw new Exception($"Couldn't find connection string for {storageType}. The connection to the database must be specified using the ConnectionBuilder method.");
        }

        internal static Func<ContextBag, DbConnection> GetConnectionBuilder<T>(this ReadOnlySettings settings)
            where T : StorageType
        {
            return GetConnectionBuilder(settings, typeof(T));
        }

    }
}