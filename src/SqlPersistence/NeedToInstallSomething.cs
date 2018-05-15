using System.Threading.Tasks;
using NServiceBus.Features;
using NServiceBus.Installation;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

class NeedToInstallSomething : INeedToInstallSomething
{
    InstallerSettings installerSettings;
    ReadOnlySettings settings;

    public NeedToInstallSomething(ReadOnlySettings settings)
    {
        this.settings = settings;
        installerSettings = settings.GetOrDefault<InstallerSettings>();
    }

    public Task Install(string identity)
    {
        if (installerSettings == null || installerSettings.Disabled)
        {
            return Task.FromResult(0);
        }

        return ScriptRunner.Install(
            sqlDialect: installerSettings.Dialect,
            tablePrefix: installerSettings.TablePrefix,
            connectionBuilder: installerSettings.ConnectionBuilder,
            scriptDirectory: installerSettings.ScriptDirectory,
            shouldInstallOutbox: settings.IsFeatureActive(typeof(SqlOutboxFeature)),
            shouldInstallSagas: settings.IsFeatureActive(typeof(SqlSagaFeature)),
            shouldInstallSubscriptions: settings.IsFeatureActive(typeof(SqlSubscriptionFeature)),
            shouldInstallTimeouts: settings.IsFeatureActive(typeof(SqlTimeoutFeature)));
    }
}
