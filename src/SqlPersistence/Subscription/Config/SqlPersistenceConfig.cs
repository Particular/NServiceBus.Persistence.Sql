using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;

namespace NServiceBus
{
    public static partial class SqlPersistenceConfig
    {

        public static SubscriptionSettings SubscriptionSettings(this PersistenceExtensions<SqlPersistence> configuration)
        {
            return new SubscriptionSettings(configuration.GetSettings());
        }

    }
}