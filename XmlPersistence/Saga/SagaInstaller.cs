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

    public Task Install(string identity)
    {
        if (!settings.ShouldInstall<StorageType.Sagas>())
        {
            return Task.FromResult(0);
        }
        var connectionString = settings.GetConnectionString<StorageType.Sagas>();
        var endpointName = settings.EndpointName().ToString();
        var sagasDirectory = Path.Combine(ScriptLocation.FindScriptDirectory(), "Sagas");
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

        return SqlHelpers.Execute(connectionString, sagaScripts, collection =>
        {
            collection.AddWithValue("schema", settings.GetSchema<StorageType.Sagas>());
            collection.AddWithValue("endpointName", endpointName);
        });
    }

}