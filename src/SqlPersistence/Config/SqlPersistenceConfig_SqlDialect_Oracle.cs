namespace NServiceBus
{
    //TODO: throw for schema in mysql
    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Configures the database schema to be used.
        /// </summary>
        public static void Schema(this SqlDialectSettings<SqlDialect.Oracle> dialectSettings, string schema)
        {
            Guard.AgainstNull(nameof(dialectSettings), dialectSettings);
            Guard.AgainstNullAndEmpty(nameof(schema), schema);
            Guard.AgainstSqlDelimiters(nameof(schema), schema);
            dialectSettings.TypedDialect.Schema = schema;
        }

        /// <summary>
        /// Increases the maximum table name size from 30 bytes to 128 bytes.
        /// </summary>
        /// <remarks>
        /// Requires Oracle 12.2 or higher.
        /// </remarks>
        public static void EnableLongTableNames(this SqlDialectSettings<SqlDialect.Oracle> dialectSettings)
        {
            Guard.AgainstNull(nameof(dialectSettings), dialectSettings);

            dialectSettings.TypedDialect.EnableLongTableNames = true;
        }
    }
}