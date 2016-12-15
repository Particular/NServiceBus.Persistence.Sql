using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Logging;
using NServiceBus.Persistence;
using NServiceBus.Settings;

class SagaInstaller : INeedToInstallSomething
{
    static ILog log = LogManager.GetLogger<SagaInstaller>();
    ReadOnlySettings settings;

    public SagaInstaller(ReadOnlySettings settings)
    {
        this.settings = settings;
    }

    public async Task Install(string identity)
    {
        if (!settings.ShouldInstall<StorageType.Sagas>())
        {
            return;
        }
        var connectionBuilder = settings.GetConnectionBuilder<StorageType.Sagas>();
        var tablePrefix = settings.GetTablePrefix<StorageType.Sagas>();

        var sqlVarient = settings.GetSqlVarient();
        var sagasDirectory = Path.Combine(ScriptLocation.FindScriptDirectory(sqlVarient), "Sagas");
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
        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync();
            await connection.ExecuteTableCommand(
                scripts: sagaScripts,
                tablePrefix: tablePrefix);
        }
    }

}