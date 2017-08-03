using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Persistence.Sql;

namespace NServiceBus
{

    public static partial class SqlPersistenceConfig
    {

        /// <summary>
        /// Exposes saga specific settings.
        /// </summary>
        public static SagaSettings SagaSettings(this PersistenceExtensions<SqlPersistence> configuration)
        {
            return new SagaSettings(configuration.GetSettings());
        }

    }
}
