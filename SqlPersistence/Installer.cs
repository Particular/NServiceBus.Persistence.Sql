using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Logging;
using NServiceBus.Persistence;
using NServiceBus.Settings;

class Installer : INeedToInstallSomething
{
    ReadOnlySettings settings;
    static ILog log = LogManager.GetLogger<Installer>();

    public Installer(ReadOnlySettings settings)
    {
        this.settings = settings;
    }

    public async Task Install(string identity)
    {
        var connectionBuilder = settings.GetConnectionBuilder();
        var sqlVariant = settings.GetSqlVariant();
        var scriptDirectory = ScriptLocation.FindScriptDirectory(sqlVariant);
        var tablePrefix = settings.GetTablePrefix();

        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync();
            using (var transaction = connection.BeginTransaction())
            {
                await InstallOutbox(scriptDirectory, connection, transaction, tablePrefix);
                await InstallSagas(scriptDirectory, connection, transaction, tablePrefix);
                await InstallSubscriptions(scriptDirectory, connection, transaction, tablePrefix);
                await InstallTimeouts(scriptDirectory, connection, transaction, tablePrefix);

                transaction.Commit();
            }
        }
    }

    Task InstallOutbox(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix)
    {
        if (!settings.ShouldInstall<StorageType.Outbox>())
        {
            return Task.FromResult(0);
        }

        var createScript = Path.Combine(scriptDirectory, "Outbox_Create.sql");
        ScriptLocation.ValidateScriptExists(createScript);
        log.Info($"Executing '{createScript}'");
        return connection.ExecuteTableCommand(
            transaction: transaction,
            script: File.ReadAllText(createScript),
            tablePrefix: tablePrefix);
    }

    Task InstallSubscriptions(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix)
    {
        if (!settings.ShouldInstall<StorageType.Subscriptions>())
        {
            return Task.FromResult(0);
        }

        var createScript = Path.Combine(scriptDirectory, "Subscription_Create.sql");
        ScriptLocation.ValidateScriptExists(createScript);
        log.Info($"Executing '{createScript}'");
        return connection.ExecuteTableCommand(
            transaction: transaction,
            script: File.ReadAllText(createScript),
            tablePrefix: tablePrefix
        );
    }

    Task InstallTimeouts(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix)
    {
        if (!settings.ShouldInstall<StorageType.Timeouts>())
        {
            return Task.FromResult(0);
        }

        var createScript = Path.Combine(scriptDirectory, "Timeout_Create.sql");
        ScriptLocation.ValidateScriptExists(createScript);
        log.Info($"Executing '{createScript}'");
        return connection.ExecuteTableCommand(
            transaction: transaction,
            script: File.ReadAllText(createScript),
            tablePrefix: tablePrefix);
    }

    Task InstallSagas(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix)
    {
        if (!settings.ShouldInstall<StorageType.Sagas>())
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
        return connection.ExecuteTableCommand(
            transaction: transaction,
            scripts: sagaScripts,
            tablePrefix: tablePrefix);
    }
}