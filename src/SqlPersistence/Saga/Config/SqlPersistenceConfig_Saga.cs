using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;

namespace NServiceBus
{
    
    public static partial class SqlPersistenceConfig
    {

        public static SagaSettings SagaSettings(this PersistenceExtensions<SqlPersistence> configuration)
        {
            return new SagaSettings(configuration.GetSettings());
        }

    }
}