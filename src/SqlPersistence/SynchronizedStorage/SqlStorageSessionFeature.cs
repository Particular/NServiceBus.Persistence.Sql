using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

sealed class SqlStorageSessionFeature : Feature
{
    public SqlStorageSessionFeature() => Defaults(s => s.SetDefault(new InstallerSettings()));

    protected override void Setup(FeatureConfigurationContext context)
    {
        // the settings are deliberately acquired here to make sure exceptions are raised in case of misconfiguration
        // during feature setup time. Later the same settings are resolved from DI to avoid allocation closures
        // everytime the scope synchronized storage session is retrieved.
        var dialect = context.Settings.GetSqlDialect();
        _ = context.Settings.GetConnectionBuilder<StorageType.Sagas>();

        var services = context.Services;
        services.AddScoped<ICompletableSynchronizedStorageSession>(provider =>
        {
            var settings = provider.GetRequiredService<IReadOnlySettings>();
            var sqlDialect = settings.GetSqlDialect();
            var connectionManager = settings.GetConnectionBuilder<StorageType.Sagas>();
            //Info cache can be null if Outbox is enabled but Sagas are disabled.
            var sagaInfoCache = provider.GetService<SagaInfoCache>();

            return new StorageSession(connectionManager, sagaInfoCache, sqlDialect);
        });
        services.AddScoped(sp => (sp.GetService<ICompletableSynchronizedStorageSession>() as ISqlStorageSession)!);

        var settings = context.Settings.Get<InstallerSettings>();
        if (!settings.Disabled)
        {
            settings.ConnectionBuilder = storageType => context.Settings.GetConnectionBuilder(storageType).BuildNonContextual();
            settings.Dialect = context.Settings.GetSqlDialect();
            settings.ScriptDirectory = ScriptLocation.FindScriptDirectory(context.Settings);
            settings.TablePrefix = context.Settings.GetTablePrefix(context.Settings.EndpointName());
            settings.IsMultiTenant = context.Settings.EndpointIsMultiTenant();

            settings.Dialect.ValidateTablePrefix(settings.TablePrefix);

            context.AddInstaller<SqlPersistenceInstaller>();
        }

        var diagnostics = dialect.GetCustomDialectDiagnosticsInfo();

        context.Settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.Sql.SqlDialect", new
        {
            dialect.Name,
            CustomDiagnostics = diagnostics
        });
    }
}