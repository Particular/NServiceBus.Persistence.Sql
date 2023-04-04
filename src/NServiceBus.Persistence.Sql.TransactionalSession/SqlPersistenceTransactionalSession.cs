namespace NServiceBus.TransactionalSession
{
    using Microsoft.Extensions.DependencyInjection;
    using Features;

    sealed class SqlPersistenceTransactionalSession : TransactionalSession
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            // can be a singleton
            context.Services
                .AddScoped<OpenSessionOptionCustomization, AmbientTransactionalSessionApplier>();

            base.Setup(context);
        }
    }
}