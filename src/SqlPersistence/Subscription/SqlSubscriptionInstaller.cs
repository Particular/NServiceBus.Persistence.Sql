using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Installation;
using NServiceBus.Logging;
using NServiceBus.Settings;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;

class SqlSubscriptionInstaller : INeedToInstallSomething
{
    SqlSubscriptionInstallerSettings installerSettings;
    ReadOnlySettings settings;
    static ILog log = LogManager.GetLogger<SqlSubscriptionInstaller>();

    public SqlSubscriptionInstaller(ReadOnlySettings settings)
    {
        this.settings = settings;
        installerSettings = settings.GetOrDefault<SqlSubscriptionInstallerSettings>();
    }

    public async Task Install(string identity)
    {
        if (installerSettings == null || installerSettings.Disabled || !settings.IsFeatureActive(typeof(SqlSubscriptionFeature)))
        {
            return;
        }

        installerSettings.Dialect.ValidateTablePrefix(installerSettings.TablePrefix);

        using (var connection = await installerSettings.ConnectionBuilder.OpenConnection().ConfigureAwait(false))
        using (var transaction = connection.BeginTransaction())
        {
            await InstallSubscriptions(installerSettings.ScriptDirectory, connection, transaction, installerSettings.TablePrefix, installerSettings.Dialect).ConfigureAwait(false);

            transaction.Commit();
        }
    }

    Task InstallSubscriptions(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
    {
        var createScript = Path.Combine(scriptDirectory, "Subscription_Create.sql");
        ScriptLocation.ValidateScriptExists(createScript);
        log.Info($"Executing '{createScript}'");

        return sqlDialect.ExecuteTableCommand(
            connection: connection,
            transaction: transaction,
            script: File.ReadAllText(createScript),
            tablePrefix: tablePrefix);
    }
}