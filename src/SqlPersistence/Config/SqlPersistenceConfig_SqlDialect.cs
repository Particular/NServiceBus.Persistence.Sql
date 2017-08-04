using System;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{
    /// <summary>
    /// Configures the table prefix to be prepended to all Saga, Timeout, Subscription and Outbox tables.
    /// </summary>
    public static partial class SqlPersistenceConfig
    {

        internal static Type GetSqlDialect(this ReadOnlySettings settings)
        {
            if (settings.TryGet("SqlPersistence.SqlDialect", out Type value))
            {
                return value;
            }
            return typeof(SqlDialect.MsSqlServer);
        }

        /// <summary>
        /// Configures hich database engine to target.
        /// </summary>
        /// <returns>Settings options available for the selected database engine.</returns>
        public static SqlDialectSettings<T> SqlDialect<T>(this PersistenceExtensions<SqlPersistence> configuration) where T : SqlDialect
        {
            var settings = configuration.GetSettings();
            settings.Set("SqlPersistence.SqlDialect", typeof(T));
            return (SqlDialectSettings<T>)Activator.CreateInstance(typeof(SqlDialectSettings<T>));
        }
    }
}