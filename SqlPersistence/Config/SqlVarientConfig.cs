using System;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{
    public static partial class SqlPersistenceConfig
    {
        public static void SqlVarient(this PersistenceExtensions<SqlPersistence> configuration, SqlVarient sqlVarient)
        {
            if (sqlVarient == Persistence.Sql.SqlVarient.All)
            {
                throw new ArgumentException("SqlVarient.All is not allowed.");
            }
            var settings = configuration.GetSettings();
            settings.Set("SqlPersistence.SqlVarient", sqlVarient);
        }

        internal static SqlVarient GetSqlVarient(this ReadOnlySettings settings)
        {
            SqlVarient value;
            if (settings.TryGet("SqlPersistence.SqlVarient", out value))
            {
                return value;
            }
            return Persistence.Sql.SqlVarient.MsSqlServer;
        }
    }
}