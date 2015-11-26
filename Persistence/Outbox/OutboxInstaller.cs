using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Installation;
using NServiceBus.Persistence;

class OutboxInstaller : IInstall
{
    BusConfiguration busConfiguration;

    public OutboxInstaller(BusConfiguration busConfiguration)
    {
        this.busConfiguration = busConfiguration;
    }

    public async Task Install(string identity)
    {
        var settings = busConfiguration.GetSettings();
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