using System;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{
    /// <summary>
    /// 
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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static SqlDialectSettings<T> Dialect<T>(this PersistenceExtensions<SqlPersistence> configuration) where T : SqlDialect
        {
            var settings = configuration.GetSettings();
            settings.Set("SqlPersistence.SqlDialect", typeof(T));
            return (SqlDialectSettings<T>)Activator.CreateInstance(typeof(SqlDialectSettings<T>));
        }
    }
}