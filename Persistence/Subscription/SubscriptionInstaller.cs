using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Installation;
using NServiceBus.Persistence;

class SubscriptionInstaller : IInstall
{
    BusConfiguration busConfiguration;

    public SubscriptionInstaller(BusConfiguration busConfiguration)
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
        var connectionString = settings.GetConnectionString<StorageType.Subscriptions>();
        var endpointName = settings.EndpointName().ToString();
        var createScript = Path.Combine(ScriptLocation.FindScriptDirectory(), "Subscription_Create.sql");

        await SqlHelpers.Execute(connectionString, File.ReadAllText(createScript), collection =>
        {
            collection.AddWithValue("schema", settings.GetSchema<StorageType.Subscriptions>());
            collection.AddWithValue("endpointName", endpointName);
        });
    }

}