using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{

    public static partial class SqlPersistenceConfig
    {
        public static void TablePrefix(this PersistenceExtensions<SqlPersistence> configuration, string tablePrefix)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            Guard.AgainstNull(nameof(tablePrefix), tablePrefix);
            Guard.AgainstSqlDelimiters(nameof(tablePrefix), tablePrefix);
            configuration.GetSettings()
                .Set("SqlPersistence.TablePrefix", tablePrefix);
        }

        internal static string GetTablePrefix(this ReadOnlySettings settings)
        {
            string tablePrefix;
            if (settings.TryGet("SqlPersistence.TablePrefix", out tablePrefix))
            {
                return tablePrefix;
            }
            var endpointName = settings.EndpointName();
            var clean = TableNameCleaner.Clean(endpointName);
            return $"{clean}_";
        }

    }
}