using System.IO;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Persistence;

class SubscriptionInstaller : INeedToInstallSomething
{

    public void Install(string identity, Configure config)
    {
        var settings = config.Settings;
        if (!settings.ShouldInstall<StorageType.Subscriptions>())
        {
            return;
        }
        var connectionString = settings.GetConnectionString<StorageType.Subscriptions>();
        var endpointName = settings.EndpointName();
        var createScript = Path.Combine(ScriptLocation.FindScriptDirectory(), "SubscriptionCreate.sql");

        SqlHelpers.Execute(connectionString, createScript, collection =>
        {
            collection.AddWithValue("schema", settings.GetSchema<StorageType.Subscriptions>());
            collection.AddWithValue("endpointName", endpointName);
        });
    }
}