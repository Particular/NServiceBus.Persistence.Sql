using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Features;
using NServiceBus.Installation;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

class SqlPersistenceInstaller(IReadOnlySettings settings) : INeedToInstallSomething
{
    public async Task Install(string identity, CancellationToken cancellationToken = default)
    {
        if (installerSettings == null || installerSettings.Disabled)
        {
            return;
        }

        await ScriptRunner.Install(
                sqlDialect: installerSettings.Dialect,
                tablePrefix: installerSettings.TablePrefix,
                connectionBuilder: installerSettings.ConnectionBuilder,
                scriptDirectory: installerSettings.ScriptDirectory,
                shouldInstallOutbox: !installerSettings.DoNotCreateOutboxTable && !installerSettings.IsMultiTenant && settings.IsFeatureActive(typeof(SqlOutboxFeature)),
                shouldInstallSagas: !installerSettings.IsMultiTenant && settings.IsFeatureActive(typeof(SqlSagaFeature)),
                shouldInstallSubscriptions: settings.IsFeatureActive(typeof(SqlSubscriptionFeature)),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    readonly InstallerSettings installerSettings = settings.GetOrDefault<InstallerSettings>();
}