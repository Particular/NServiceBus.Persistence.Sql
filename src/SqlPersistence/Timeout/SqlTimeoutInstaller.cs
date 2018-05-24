using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Installation;
using NServiceBus.Logging;
using NServiceBus.Settings;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;

class SqlTimeoutInstaller : INeedToInstallSomething
{
    SqlTimeoutInstallerSettings installerSettings;
    ReadOnlySettings settings;
    static ILog log = LogManager.GetLogger<SqlTimeoutInstaller>();

    public SqlTimeoutInstaller(ReadOnlySettings settings)
    {
        this.settings = settings;
        installerSettings = settings.GetOrDefault<SqlTimeoutInstallerSettings>();
    }

    public async Task Install(string identity)
    {
        if (installerSettings == null || installerSettings.Disabled || !settings.IsFeatureActive(typeof(SqlTimeoutFeature)))
        {
            return;
        }

        installerSettings.Dialect.ValidateTablePrefix(installerSettings.TablePrefix);

        using (var connection = await installerSettings.ConnectionBuilder.OpenConnection().ConfigureAwait(false))
        using (var transaction = connection.BeginTransaction())
        {
            await InstallTimeouts(installerSettings.ScriptDirectory, connection, transaction, installerSettings.TablePrefix, installerSettings.Dialect).ConfigureAwait(false);

            transaction.Commit();
        }
    }
    
    Task InstallTimeouts(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
    {
        var createScript = Path.Combine(scriptDirectory, "Timeout_Create.sql");
        ScriptLocation.ValidateScriptExists(createScript);
        log.Info($"Executing '{createScript}'");

        return sqlDialect.ExecuteTableCommand(
            connection: connection,
            transaction: transaction,
            script: File.ReadAllText(createScript),
            tablePrefix: tablePrefix);
    }
}