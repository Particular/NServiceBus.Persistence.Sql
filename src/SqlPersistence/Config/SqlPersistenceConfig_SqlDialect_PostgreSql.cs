namespace NServiceBus
{
    using System;
    using System.Data.Common;

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Configures the database schema to be used.
        /// </summary>
        public static void Schema(this SqlDialectSettings<SqlDialect.PostgreSql> dialectSettings, string schema)
        {
            ArgumentNullException.ThrowIfNull(dialectSettings);
            ArgumentException.ThrowIfNullOrWhiteSpace(schema);
            Guard.AgainstSqlDelimiters(nameof(schema), schema);
            dialectSettings.TypedDialect.Schema = schema;
        }

        /// <summary>
        /// Sets a <see cref="Action"/> used to modify a <see cref="DbParameter"/> when being used for storing JsonB.
        /// </summary>
        public static void JsonBParameterModifier(this SqlDialectSettings<SqlDialect.PostgreSql> dialectSettings, Action<DbParameter> modifier)
        {
            ArgumentNullException.ThrowIfNull(dialectSettings);
            ArgumentNullException.ThrowIfNull(modifier);
            dialectSettings.TypedDialect.JsonBParameterModifier = modifier;
        }

        /// <summary>
        /// Instructs the persistence to not use the connection established by the PostgreSQL transport.
        /// </summary>
        public static void DoNotUsePostgreSqlTransportConnection(this SqlDialectSettings<SqlDialect.PostgreSql> dialectSettings)
        {
            ArgumentNullException.ThrowIfNull(dialectSettings);
            dialectSettings.TypedDialect.DoNotUseTransportConnection = true;
        }
    }
}