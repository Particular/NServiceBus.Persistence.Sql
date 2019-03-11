using System;
using System.Threading.Tasks;
using NServiceBus.Features;
using NServiceBus.Installation;
using NServiceBus.Logging;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

class SqlPersistenceInstaller : INeedToInstallSomething
{
    static ILog log = LogManager.GetLogger<SqlPersistenceInstaller>();

    InstallerSettings installerSettings;
    ReadOnlySettings settings;

    public SqlPersistenceInstaller(ReadOnlySettings settings)
    {
        this.settings = settings;
        installerSettings = settings.GetOrDefault<InstallerSettings>();
    }

    public async Task Install(string identity)
    {
        if (installerSettings == null || installerSettings.Disabled)
        {
            return;
        }
        try
        {
            await ScriptRunner.Install(
                    sqlDialect: installerSettings.Dialect,
                    tablePrefix: installerSettings.TablePrefix,
                    connectionBuilder: installerSettings.ConnectionBuilder,
                    scriptDirectory: installerSettings.ScriptDirectory,
                    shouldInstallOutbox: !installerSettings.IsMultiTenant && settings.IsFeatureActive(typeof(SqlOutboxFeature)),
                    shouldInstallSagas: !installerSettings.IsMultiTenant && settings.IsFeatureActive(typeof(SqlSubscriptionFeature)),
                    shouldInstallSubscriptions: settings.IsFeatureActive(typeof(SqlSubscriptionFeature)),
                    shouldInstallTimeouts: settings.IsFeatureActive(typeof(SqlTimeoutFeature)))
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            log.Error("Could not complete the installation. ", e);
            throw;
        }
    }
}
