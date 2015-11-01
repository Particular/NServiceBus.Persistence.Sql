using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Persistence;

class OutboxInstaller : INeedToInstallSomething
{

    public async Task InstallAsync(string identity, Configure config)
    {
        var settings = config.Settings;
        if (!settings.ShouldInstall<StorageType.Outbox>())
        {
            return;
        }
        var connectionString = settings.GetConnectionString<StorageType.Outbox>();
        var endpointName = settings.EndpointName().ToString();
        var createScript = Path.Combine(ScriptLocation.FindScriptDirectory(), "Outbox_Create.sql");

        await SqlHelpers.Execute(connectionString, File.ReadAllText(createScript), collection =>
        {
            collection.AddWithValue("schema", settings.GetSchema<StorageType.Outbox>());
            collection.AddWithValue("endpointName", endpointName);
        });
    }
    
}