using System;
using System.Collections.Generic;
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

        var prefix = installerSettings.TablePrefix;
        var dialect = installerSettings.Dialect;
        dialect.ValidateTablePrefix(prefix);
        var scriptDirectory = installerSettings.ScriptDirectory;
        using (var connection = await installerSettings.ConnectionBuilder.OpenConnection().ConfigureAwait(false))
        using (var transaction = connection.BeginTransaction())
        {
            var tasks = new List<Task>
            {
                InstallOutbox(scriptDirectory, connection, transaction, prefix, dialect),
                InstallSubscriptions(scriptDirectory, connection, transaction, prefix, dialect),
                InstallTimeouts(scriptDirectory, connection, transaction, prefix, dialect)
            };
            tasks.AddRange(InstallSagas(scriptDirectory, connection, transaction, prefix, dialect));

            await Task.WhenAll(tasks).ConfigureAwait(false);
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

    IEnumerable<Task> InstallSagas(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
    {
        if (!settings.IsFeatureActive(typeof(SqlSagaFeature)))
        {
            return Enumerable.Empty<Task>();
        }

        var sagasDirectory = Path.Combine(scriptDirectory, "Sagas");
        if (!Directory.Exists(sagasDirectory))
        {
            log.Info($"Diretory '{sagasDirectory}' not found so no saga creation scripts will be executed.");
            return Enumerable.Empty<Task>();
        }
        var scriptFiles = Directory.EnumerateFiles(sagasDirectory, "*_Create.sql").ToList();
        log.Info($@"Executing saga creation scripts:
{string.Join(Environment.NewLine, scriptFiles)}");

        return scriptFiles
            .Select(scriptFile => Execute(scriptFile, connection, transaction, tablePrefix, sqlDialect));
    }

    Task Execute(string scriptFile, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
    {
        var script = File.ReadAllText(scriptFile);
        return sqlDialect.ExecuteTableCommand(connection, transaction, script, tablePrefix);
    }
}