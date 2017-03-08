using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{

    public static partial class SqlPersistenceConfig
    {

        public static void Schema(this PersistenceExtensions<SqlPersistence> configuration, string schema)
        {
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