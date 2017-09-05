using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
            var scripts = new List<string>();
            AddOutboxScript(scriptDirectory, scripts);
            AddSubscriptionsScript(scriptDirectory, scripts);
            AddTimeoutsSctipt(scriptDirectory, scripts);
            AddSagaScripts(scriptDirectory, scripts);
            var builder = new SqlConnectionStringBuilder(connection.ConnectionString);
            if (builder.MultipleActiveResultSets)
            {
                log.Info("Executing using MultipleActiveResultSets");
                var tasks = scripts.Select(script =>
                {
                    log.Info($"Executing '{script}'");
                    var allText = File.ReadAllText(script);
                    return dialect.ExecuteTableCommand(connection, transaction, allText, prefix);
                });
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                foreach (var script in scripts)
                {
                    log.Info($"Executing '{script}'");
                    var allText = File.ReadAllText(script);
                    await dialect.ExecuteTableCommand(connection, transaction, allText, prefix).ConfigureAwait(false);
                }
            }
            transaction.Commit();
        }
    }

    void AddOutboxScript(string scriptDirectory, List<string> scripts)
    {
        if (!settings.IsFeatureActive(typeof(SqlOutboxFeature)))
        {
            return;
        }

        var createScript = Path.Combine(scriptDirectory, "Outbox_Create.sql");
        ScriptLocation.ValidateScriptExists(createScript);
        log.Info($"The outbox script will be executed '{createScript}'");

        scripts.Add(createScript);
    }

    void AddSubscriptionsScript(string scriptDirectory, List<string> scripts)
    {
        if (!settings.IsFeatureActive(typeof(SqlSubscriptionFeature)))
        {
            return;
        }

        var createScript = Path.Combine(scriptDirectory, "Subscription_Create.sql");
        ScriptLocation.ValidateScriptExists(createScript);
        log.Info($"The subscription script will be executed '{createScript}'");

        scripts.Add(createScript);
    }

    void AddTimeoutsSctipt(string scriptDirectory, List<string> scripts)
    {
        if (!settings.IsFeatureActive(typeof(SqlTimeoutFeature)))
        {
            return;
        }

        var createScript = Path.Combine(scriptDirectory, "Timeout_Create.sql");
        ScriptLocation.ValidateScriptExists(createScript);
        log.Info($"The timeout script will be executed '{createScript}'");

        scripts.Add(createScript);
    }

    void AddSagaScripts(string scriptDirectory, List<string> scripts)
    {
        if (!settings.IsFeatureActive(typeof(SqlSagaFeature)))
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
        log.Info($@"The saga scripts will be executed:
{string.Join(Environment.NewLine, scriptFiles)}");

        scripts.AddRange(scriptFiles);
    }

}