using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Logging;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

class Installer : INeedToInstallSomething
{
    ReadOnlySettings settings;
    static ILog log = LogManager.GetLogger<Installer>();
    InstallerSettings installerSettings;

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

        var connectionBuilder = installerSettings.ConnectionBuilder;
        var sqlVariant = installerSettings.SqlVariant;
        var schema = settings.GetSchema();
        var scriptDirectory = installerSettings.ScriptDirectory;
        var tablePrefix = installerSettings.TablePrefix;

        ConfigValidation.ValidateTableSettings(sqlVariant, tablePrefix, schema);

        using (var connection = await connectionBuilder.OpenConnection().ConfigureAwait(false))
        using (var transaction = connection.BeginTransaction())
        {
            await InstallOutbox(scriptDirectory, connection, transaction, tablePrefix, schema, sqlVariant).ConfigureAwait(false);
            await InstallSagas(scriptDirectory, connection, transaction, tablePrefix, schema, sqlVariant).ConfigureAwait(false);
            await InstallSubscriptions(scriptDirectory, connection, transaction, tablePrefix, schema, sqlVariant).ConfigureAwait(false);
            await InstallTimeouts(scriptDirectory, connection, transaction, tablePrefix, schema, sqlVariant).ConfigureAwait(false);

            transaction.Commit();
        }
    }

    Task InstallOutbox(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, string schema, SqlVariant sqlVariant)
    {
        if (!settings.GetFeatureEnabled<StorageType.Outbox>())
        {
            return Task.FromResult(0);
        }

        var createScript = Path.Combine(scriptDirectory, "Outbox_Create.sql");
        ScriptLocation.ValidateScriptExists(createScript);
        log.Info($"Executing '{createScript}'");
        if (sqlVariant == SqlVariant.Oracle)
        {
            return connection.ExecuteTableCommand(
                transaction: transaction,
                script: File.ReadAllText(createScript),
                tablePrefix: tablePrefix);
        }
        else
        {
            return connection.ExecuteTableCommand(
                transaction: transaction,
                script: File.ReadAllText(createScript),
                tablePrefix: tablePrefix,
                schema: schema);
        }
    }

    Task InstallSubscriptions(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, string schema, SqlVariant sqlVariant)
    {
        if (!settings.GetFeatureEnabled<StorageType.Subscriptions>())
        {
            return Task.FromResult(0);
        }

        var createScript = Path.Combine(scriptDirectory, "Subscription_Create.sql");
        ScriptLocation.ValidateScriptExists(createScript);
        log.Info($"Executing '{createScript}'");
        if (sqlVariant == SqlVariant.Oracle)
        {
            return connection.ExecuteTableCommand(
                transaction: transaction,
                script: File.ReadAllText(createScript),
                tablePrefix: tablePrefix);
        }
        else
        {
            return connection.ExecuteTableCommand(
                transaction: transaction,
                script: File.ReadAllText(createScript),
                tablePrefix: tablePrefix,
                schema: schema);
        }
    }

    Task InstallTimeouts(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, string schema, SqlVariant sqlVariant)
    {
        if (!settings.GetFeatureEnabled<StorageType.Timeouts>())
        {
            return Task.FromResult(0);
        }

        var createScript = Path.Combine(scriptDirectory, "Timeout_Create.sql");
        ScriptLocation.ValidateScriptExists(createScript);
        log.Info($"Executing '{createScript}'");
        if (sqlVariant == SqlVariant.Oracle)
        {
            return connection.ExecuteTableCommand(
                transaction: transaction,
                script: File.ReadAllText(createScript),
                tablePrefix: tablePrefix);
        }
        else
        {
            return connection.ExecuteTableCommand(
                transaction: transaction,
                script: File.ReadAllText(createScript),
                tablePrefix: tablePrefix,
                schema: schema);
        }
    }

    Task InstallSagas(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, string schema, SqlVariant sqlVariant)
    {
        if (!settings.GetFeatureEnabled<StorageType.Sagas>())
        {
            return Task.FromResult(0);
        }

        var sagasDirectory = Path.Combine(scriptDirectory, "Sagas");
        if (!Directory.Exists(sagasDirectory))
        {
            log.Info($"Diretory '{sagasDirectory}' not found so no saga creation scripts will be executed.");
            return Task.FromResult(0);
        }
        var scriptFiles = Directory.EnumerateFiles(sagasDirectory, "*_Create.sql").ToList();
        log.Info($@"Executing saga creation scripts:
{string.Join(Environment.NewLine, scriptFiles)}");
        var sagaScripts = scriptFiles
            .Select(File.ReadAllText);
        if (sqlVariant == SqlVariant.Oracle)
        {
            return connection.ExecuteTableCommand(
                transaction: transaction,
                scripts: sagaScripts);
        }
        else
        {
            return connection.ExecuteTableCommand(
                transaction: transaction,
                scripts: sagaScripts,
                tablePrefix: tablePrefix,
                schema: schema);
        }
    }
}