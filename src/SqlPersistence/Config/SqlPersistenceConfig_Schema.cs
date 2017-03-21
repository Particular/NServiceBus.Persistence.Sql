using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{

    //TODO: throw for schema in mysql
    public static partial class SqlPersistenceConfig
    {

        /// <summary>
        /// Configures the database schema to be used.
        /// </summary>
        public static void Schema(this PersistenceExtensions<SqlPersistence> configuration, string schema)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNullAndEmpty(nameof(schema), schema);
            Guard.AgainstSqlDelimiters(nameof(schema), schema);
            configuration.GetSettings()
                .Set("SqlPersistence.Schema", schema);
        }

        internal static string GetSchema(this ReadOnlySettings settings)
        {
            string schema;
            if (settings.TryGet("SqlPersistence.Schema", out schema))
            {
                return schema;
            }
            var sqlVariant = settings.GetSqlVariant();
            if (sqlVariant == Persistence.Sql.SqlVariant.MsSqlServer)
            {
                return "dbo";
            }
            return null;
        }

    }
}