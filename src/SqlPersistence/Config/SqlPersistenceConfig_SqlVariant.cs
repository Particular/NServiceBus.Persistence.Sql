using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{
    public static partial class SqlPersistenceConfig
    {
        public static void SqlVariant(this PersistenceExtensions<SqlPersistence> configuration, SqlVariant sqlVariant)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            var settings = configuration.GetSettings();
            settings.Set("SqlPersistence.SqlVariant", sqlVariant);
        }

        internal static SqlVariant GetSqlVariant(this ReadOnlySettings settings)
        {
            SqlVariant value;
            if (settings.TryGet("SqlPersistence.SqlVariant", out value))
            {
                return value;
            }
            return Persistence.Sql.SqlVariant.MsSqlServer;
        }
    }
}
