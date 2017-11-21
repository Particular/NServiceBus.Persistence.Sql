namespace NServiceBus
{
    using System;
    using Configuration.AdvanceExtensibility;
    using Persistence.Sql;
    using Settings;

    public static partial class SqlPersistenceConfig
    {
        internal static SqlDialect GetSqlDialect(this ReadOnlySettings settings)
        {
            if (settings.TryGet("SqlPersistence.SqlDialect", out SqlDialectSettings value))
            {
                return value.Dialect;
            }
            throw new Exception($"Must specify a SQL dialect using persistence.{nameof(SqlDialect)}<T>() method.");
        }

        /// <summary>
        /// Configures which database engine to target.
        /// </summary>
        /// <returns>Settings options available for the selected database engine.</returns>
        public static SqlDialectSettings<T> SqlDialect<T>(this PersistenceExtensions<SqlPersistence> configuration)
            where T : SqlDialect, new()
        {
            var settings = configuration.GetSettings();

            if (settings.TryGet("SqlPersistence.SqlDialect", out SqlDialectSettings<T> dialectSettings))
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