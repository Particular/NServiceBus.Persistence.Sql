using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Persistence;

class SubscriptionInstaller : INeedToInstallSomething
{

    public async Task InstallAsync(string identity, Configure config)
    {
        var settings = config.Settings;
        if (!settings.ShouldInstall<StorageType.Subscriptions>())
        {
            return;
        }
        var connectionString = settings.GetConnectionString<StorageType.Subscriptions>();
        var endpointName = settings.EndpointName();
        var createScript = Path.Combine(ScriptLocation.FindScriptDirectory(), "Subscription_Create.sql");

        await SqlHelpers.Execute(connectionString, File.ReadAllText(createScript), collection =>
        {
            collection.AddWithValue("schema", settings.GetSchema<StorageType.Subscriptions>());
            collection.AddWithValue("endpointName", endpointName);
        });
    }

}