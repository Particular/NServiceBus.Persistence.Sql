using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Persistence;

class SagaInstaller : INeedToInstallSomething
{

    public async Task InstallAsync(string identity, Configure config)
    {
        var settings = config.Settings;
        if (!settings.GetFeatureEnabled<StorageType.Sagas>())
        {
            return;
        }
        if (settings.GetDisableInstaller<StorageType.Sagas>())
        {
            return;
        }
        var connectionString = settings.GetConnectionString<StorageType.Sagas>();
        var endpointName = config.Settings.EndpointName().ToString();
        var sagasDirectory = Path.Combine(ScriptLocation.FindScriptDirectory(), "Sagas");
        var sagaScripts = Directory.EnumerateFiles(sagasDirectory, "*_Create.sql")
            .Select(File.ReadAllText);

        await SqlHelpers.Execute(connectionString, sagaScripts, collection =>
        {
            collection.AddWithValue("schema", settings.GetSchema<StorageType.Sagas>());
            collection.AddWithValue("endpointName", endpointName);
        });
    }

}