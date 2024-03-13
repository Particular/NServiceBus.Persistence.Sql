namespace NServiceBus
{
    using System;

    //TODO: throw for schema in mysql
    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Configures the database schema to be used.
        /// </summary>
        public static void Schema(this SqlDialectSettings<SqlDialect.Oracle> dialectSettings, string schema)
        {
            ArgumentNullException.ThrowIfNull(dialectSettings);
            ArgumentException.ThrowIfNullOrWhiteSpace(schema);
            Guard.AgainstSqlDelimiters(nameof(schema), schema);
            dialectSettings.TypedDialect.Schema = schema;
        }
    }
}