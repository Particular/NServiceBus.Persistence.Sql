namespace NServiceBus
{
    using System;

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Configures the database schema to be used.
        /// </summary>
        public static void Schema(this SqlDialectSettings<SqlDialect.MsSqlServer> dialectSettings, string schema)
        {
            ArgumentNullException.ThrowIfNull(dialectSettings);
            ArgumentException.ThrowIfNullOrWhiteSpace(schema);
            Guard.AgainstSqlDelimiters(nameof(schema), schema);
            dialectSettings.TypedDialect.Schema = schema;
        }

        /// <summary>
        /// Instructs the persistence to not use the connection established by the SQL Server transport.
        /// </summary>
        public static void DoNotUseSqlServerTransportConnection(this SqlDialectSettings<SqlDialect.MsSqlServer> dialectSettings)
        {
            ArgumentNullException.ThrowIfNull(dialectSettings);
            dialectSettings.TypedDialect.DoNotUseTransportConnection = true;
        }
    }
}