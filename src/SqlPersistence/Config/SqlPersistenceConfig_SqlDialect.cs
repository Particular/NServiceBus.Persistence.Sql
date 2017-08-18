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

        internal static SqlDialect GetSqlDialect(this ReadOnlySettings settings)
        {
            if (settings.TryGet("SqlPersistence.SqlDialect", out SqlDialectSettings value))
            {
                return value.Dialect;
            }
            throw new Exception("Must specify SQL dialect using persistence.SqlDialect<T>() method.");
        }

        /// <summary>
        /// Configures which database engine to target.
        /// </summary>
        /// <returns>Settings options available for the selected database engine.</returns>
        public static SqlDialectSettings<T> SqlDialect<T>(this PersistenceExtensions<SqlPersistence> configuration) where T : SqlDialect, new()
        {
            var settings = configuration.GetSettings();

            SqlDialectSettings<T> dialectSettings;
            if (settings.TryGet("SqlPersistence.SqlDialect", out dialectSettings))
            {
                return dialectSettings;
            }

            var type = typeof(SqlDialectSettings<>).MakeGenericType(typeof(T));
            dialectSettings = (SqlDialectSettings<T>)Activator.CreateInstance(type);
            settings.Set("SqlPersistence.SqlDialect", dialectSettings);
            return dialectSettings;
        }
    }
}