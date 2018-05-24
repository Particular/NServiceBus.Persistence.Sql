namespace NServiceBus
{
    using System;
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
                .Set("SqlPersistence.ConnectionBuilder", connectionBuilder);
        }

        /// <summary>
        /// Configures how <see cref="DbConnection"/>s are constructed for StorageType.
        /// </summary>
        public static void ConnectionBuilder<T>(this PersistenceExtensions<SqlPersistence> configuration, Func<DbConnection> connectionBuilder) where T : Persistence.StorageType
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(connectionBuilder), connectionBuilder);

            configuration.GetSettings()
                .Set($"SqlPersistence.{typeof(T).Name}.ConnectionBuilder", connectionBuilder);
        }

        internal static Func<DbConnection> GetConnectionBuilder(this ReadOnlySettings settings)
        {
            if (settings.TryGet("SqlPersistence.ConnectionBuilder", out Func<DbConnection> value))
            {
                return value;
            }
            throw new Exception("ConnectionBuilder must be defined.");
        }

        internal static Func<DbConnection> GetConnectionBuilder<T>(this ReadOnlySettings settings) where T : Persistence.StorageType
        {
            if (settings.TryGet($"SqlPersistence.{typeof(T).Name}.ConnectionBuilder", out Func<DbConnection> value))
            {
                return value;
            }

            return settings.GetConnectionBuilder();
        }
    }
}