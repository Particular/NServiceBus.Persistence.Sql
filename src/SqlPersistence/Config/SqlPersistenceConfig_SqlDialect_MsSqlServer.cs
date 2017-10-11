namespace NServiceBus
{
    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Configures the database schema to be used.
        /// </summary>
        public static void Schema(this SqlDialectSettings<SqlDialect.MsSqlServer> dialectSettings, string schema)
        {
            Guard.AgainstNull(nameof(dialectSettings), dialectSettings);
            Guard.AgainstNullAndEmpty(nameof(schema), schema);
            Guard.AgainstSqlDelimiters(nameof(schema), schema);
            dialectSettings.TypedDialect.Schema = schema;
        }

        /// <summary>
        /// Instructs the persistence to not use the connection established by the SQL Server transport.
        /// </summary>
        public static void DoNotUseSqlServerTransportConnection(this SqlDialectSettings<SqlDialect.MsSqlServer> dialectSettings)
        {
            Guard.AgainstNull(nameof(dialectSettings), dialectSettings);
            dialectSettings.TypedDialect.DoNotUseTransportConnection = true;
        }
    }
}