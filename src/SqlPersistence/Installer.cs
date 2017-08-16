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

        ConfigValidation.ValidateTableSettings(sqlDialect, tablePrefix);

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
        if (sqlDialect is SqlDialect.Oracle)
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
                schema: sqlDialect.Schema);
        }
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
        if (sqlDialect is SqlDialect.Oracle)
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
                schema: sqlDialect.Schema);
        }
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
        if (sqlDialect is SqlDialect.Oracle)
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
                schema: sqlDialect.Schema);
        }
    }

    Task InstallSagas(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
    {
        if (!settings.ShouldInstall<SqlSagaFeature>())
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
        if (sqlDialect is SqlDialect.Oracle)
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
                schema: sqlDialect.Schema);
        }
    }
}