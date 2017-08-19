using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Logging;
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
        var sqlDialect = settings.GetSqlDialect();
        var scriptDirectory = ScriptLocation.FindScriptDirectory(sqlDialect);
        var tablePrefix = settings.GetTablePrefix();

        sqlDialect.ValidateTablePrefix(tablePrefix);

        using (var connection = await connectionBuilder.OpenConnection().ConfigureAwait(false))
        using (var transaction = connection.BeginTransaction())
        {
            await InstallOutbox(scriptDirectory, connection, transaction, tablePrefix, sqlDialect).ConfigureAwait(false);
            await InstallSagas(scriptDirectory, connection, transaction, tablePrefix, sqlDialect).ConfigureAwait(false);
            await InstallSubscriptions(scriptDirectory, connection, transaction, tablePrefix, sqlDialect).ConfigureAwait(false);
            await InstallTimeouts(scriptDirectory, connection, transaction, tablePrefix, sqlDialect).ConfigureAwait(false);

            transaction.Commit();
        }
    }

    Task InstallOutbox(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
    {
        if (!settings.ShouldInstall<SqlOutboxFeature>())
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
        if (!settings.ShouldInstall<SqlSubscriptionFeature>())
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
        if (!settings.ShouldInstall<SqlTimeoutFeature>())
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
        if (!settings.ShouldInstall<SqlSagaFeature>())
        {
            return;
        }

        var sagasDirectory = Path.Combine(scriptDirectory, "Sagas");
        if (!Directory.Exists(sagasDirectory))
        {
            log.Info($"Diretory '{sagasDirectory}' not found so no saga creation scripts will be executed.");
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