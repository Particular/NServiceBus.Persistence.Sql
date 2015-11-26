using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Installation;
using NServiceBus.Persistence;

class TimeoutInstaller : IInstall
{
    BusConfiguration busConfiguration;

    public TimeoutInstaller(BusConfiguration busConfiguration)
    {
        this.busConfiguration = busConfiguration;
    }

    public async Task Install(string identity)
    {
        var settings = busConfiguration.GetSettings();
        if (!settings.ShouldInstall<StorageType.Subscriptions>())
        {
            return;
        }
        var connectionString = settings.GetConnectionString<StorageType.Timeouts>();
        var endpointName = settings.EndpointName().ToString();
        var createScript = Path.Combine(ScriptLocation.FindScriptDirectory(), "Timeout_Create.sql");

        await SqlHelpers.Execute(connectionString, File.ReadAllText(createScript), collection =>
        {
            collection.AddWithValue("schema", settings.GetSchema<StorageType.Timeouts>());
            collection.AddWithValue("endpointName", endpointName);
        });
    }
    
}