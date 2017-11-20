namespace NServiceBus
{
    using System;
    using System.Data.Common;
    using Configuration.AdvanceExtensibility;
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

        internal static Func<DbConnection> GetConnectionBuilder(this ReadOnlySettings settings)
        {
            if (settings.TryGet("SqlPersistence.ConnectionBuilder", out Func<DbConnection> value))
            {
                return value;
            }
            throw new Exception("ConnectionBuilder must be defined.");
        }

    }
}