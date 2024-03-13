namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Settings;

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Configures the table prefix to be prepended to all Saga, Timeout, Subscription and Outbox tables.
        /// </summary>
        public static void TablePrefix(this PersistenceExtensions<SqlPersistence> configuration, string tablePrefix)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(tablePrefix);
            Guard.AgainstSqlDelimiters(nameof(tablePrefix), tablePrefix);
            configuration.GetSettings()
                .Set("SqlPersistence.TablePrefix", tablePrefix);
        }

        internal static string GetTablePrefix(this IReadOnlySettings settings)
        {
            if (settings.TryGet("SqlPersistence.TablePrefix", out string tablePrefix))
            {
                return tablePrefix;
            }
            var endpointName = settings.EndpointName();
            var clean = TableNameCleaner.Clean(endpointName);
            return $"{clean}_";
        }

    }
}