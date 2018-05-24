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

class SqlSagaInstaller : INeedToInstallSomething
{
    SqlSagaInstallerSettings installerSettings;
    ReadOnlySettings settings;
    static ILog log = LogManager.GetLogger<SqlSagaInstaller>();

    public SqlSagaInstaller(ReadOnlySettings settings)
    {
        this.settings = settings;
        installerSettings = settings.GetOrDefault<SqlSagaInstallerSettings>();
    }

    public async Task Install(string identity)
    {
        if (installerSettings == null || installerSettings.Disabled || !settings.IsFeatureActive(typeof(SqlSagaFeature)))
        {
            return;
        }

        installerSettings.Dialect.ValidateTablePrefix(installerSettings.TablePrefix);

        using (var connection = await installerSettings.ConnectionBuilder.OpenConnection().ConfigureAwait(false))
        using (var transaction = connection.BeginTransaction())
        {
            await InstallSagas(installerSettings.ScriptDirectory, connection, transaction, installerSettings.TablePrefix, installerSettings.Dialect).ConfigureAwait(false);

            transaction.Commit();
        }
    }

    async Task InstallSagas(string scriptDirectory, DbConnection connection, DbTransaction transaction, string tablePrefix, SqlDialect sqlDialect)
    {
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