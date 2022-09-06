namespace NServiceBus.TransactionalSession
{
    using Features;

    sealed class SqlPersistenceTransactionalSession : Feature
    {
        public SqlPersistenceTransactionalSession()
        {
            Defaults(s => s.EnableFeatureByDefault<TransactionalSession>());

            DependsOn<SynchronizedStorage>();
            DependsOn<TransactionalSession>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {

        }
    }
}