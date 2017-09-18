using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Installation;
using NServiceBus.Logging;
using NServiceBus.Settings;

class Installer : INeedToInstallSomething
{
    InstallerSettings installerSettings;
    ReadOnlySettings settings;
    static ILog log = LogManager.GetLogger<Installer>();

    public Installer(ReadOnlySettings settings)
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

        installerSettings.Dialect.ValidateTablePrefix(installerSettings.TablePrefix);

        using (var connection = await installerSettings.ConnectionBuilder.OpenConnection().ConfigureAwait(false))
        using (var transaction = connection.BeginTransaction())
        {
            await InstallOutbox(installerSettings.ScriptDirectory, connection, transaction, installerSettings.TablePrefix, installerSettings.Dialect).ConfigureAwait(false);
            await InstallSagas(installerSettings.ScriptDirectory, connection, transaction, installerSettings.TablePrefix, installerSettings.Dialect).ConfigureAwait(false);
            await InstallSubscriptions(installerSettings.ScriptDirectory, connection, transaction, installerSettings.TablePrefix, installerSettings.Dialect).ConfigureAwait(false);
            await InstallTimeouts(installerSettings.ScriptDirectory, connection, transaction, installerSettings.TablePrefix, installerSettings.Dialect).ConfigureAwait(false);

            transaction.Commit();
        }
    }
    
    Task InstallOutbox(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
    {
        if (!settings.IsFeatureActive(typeof(SqlOutboxFeature)))
        {
            return Task.FromResult(0);
        }

        var createScript = Path.Combine(scriptDirectory, "Outbox_Create.sql");
        ScriptLocation.ValidateScriptExists(createScript);
        log.Info($"Executing '{createScript}'");

        return sqlDialect.ExecuteTableCommand(
            connection: connection,
            transaction: transaction,
            script: File.ReadAllText(createScript),
            tablePrefix: tablePrefix);
    }

    Task InstallSubscriptions(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
    {
        if (!settings.IsFeatureActive(typeof(SqlSubscriptionFeature)))
        {
            return Task.FromResult(0);
        }

        var createScript = Path.Combine(scriptDirectory, "Subscription_Create.sql");
        ScriptLocation.ValidateScriptExists(createScript);
        log.Info($"Executing '{createScript}'");

        return sqlDialect.ExecuteTableCommand(
            connection: connection,
            transaction: transaction,
            script: File.ReadAllText(createScript),
            tablePrefix: tablePrefix);
    }

    Task InstallTimeouts(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
    {
        if (!settings.IsFeatureActive(typeof(SqlTimeoutFeature)))
        {
            return Task.FromResult(0);
        }

        var createScript = Path.Combine(scriptDirectory, "Timeout_Create.sql");
        ScriptLocation.ValidateScriptExists(createScript);
        log.Info($"Executing '{createScript}'");

        return sqlDialect.ExecuteTableCommand(
            connection: connection,
            transaction: transaction,
            script: File.ReadAllText(createScript),
            tablePrefix: tablePrefix);
    }

    async Task InstallSagas(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
    {
        if (!settings.IsFeatureActive(typeof(SqlSagaFeature)))
        {
            return;
        }

        var sagasDirectory = Path.Combine(scriptDirectory, "Sagas");
        if (!Directory.Exists(sagasDirectory))
        {
            log.Info($"Directory '{sagasDirectory}' not found so no saga creation scripts will be executed.");
            return;
        }
        var scriptFiles = Directory.EnumerateFiles(sagasDirectory, "*_Create.sql").ToList();
        log.Info($@"Executing saga creation scripts:
{string.Join(Environment.NewLine, scriptFiles)}");
        var sagaScripts = scriptFiles
            .Select(File.ReadAllText);

        foreach (var script in sagaScripts)
        {
            await sqlDialect.ExecuteTableCommand(connection, transaction, script, tablePrefix).ConfigureAwait(false);
        }
    }
}