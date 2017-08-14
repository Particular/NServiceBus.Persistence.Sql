using System;
using NServiceBus.Persistence.Sql;

namespace NServiceBus
{
    //TODO: throw for schema in mysql
    public static partial class SqlPersistenceConfig
    {

        /// <summary>
        /// Obsolete: Use 'persistence.UseSchema&lt;SqlDialect.DialectType&gt;().Schema("schema_name")' instead. Will be removed in version 4.0.0.
        /// </summary>
        [Obsolete("Use 'persistence.UseSchema<SqlDialect.DialectType>().Schema(\"schema_name\")' instead. Will be removed in version 4.0.0.", true)]
        public static void Schema(this PersistenceExtensions<SqlPersistence> configuration, string schema)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Configures the database schema to be used.
        /// </summary>
        public static void Schema(this SqlDialectSettings<SqlDialect.MsSqlServer> dialectSettings, string schema)
        {
            Guard.AgainstNull(nameof(dialectSettings), dialectSettings);
            Guard.AgainstNullAndEmpty(nameof(schema), schema);
            Guard.AgainstSqlDelimiters(nameof(schema), schema);
            dialectSettings.Settings.Schema = schema;
        }

    }
}