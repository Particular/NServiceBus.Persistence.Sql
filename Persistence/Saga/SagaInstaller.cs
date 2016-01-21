using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Persistence;
using NServiceBus.Settings;

class SagaInstaller : INeedToInstallSomething
{
    ReadOnlySettings settings;

    public SagaInstaller(ReadOnlySettings settings)
    {
        this.settings = settings;
    }

    public async Task Install(string identity)
    {
        if (!settings.GetFeatureEnabled<StorageType.Sagas>())
        {
            return;
        }
        if (settings.GetDisableInstaller<StorageType.Sagas>())
        {
            return;
        }
        var connectionString = settings.GetConnectionString<StorageType.Sagas>();
        var endpointName = settings.EndpointName().ToString();
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