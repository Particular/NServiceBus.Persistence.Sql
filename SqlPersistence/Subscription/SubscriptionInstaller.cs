using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Logging;
using NServiceBus.Persistence;
using NServiceBus.Settings;

class SubscriptionInstaller : INeedToInstallSomething
{
    static ILog log = LogManager.GetLogger<SubscriptionInstaller>();
    ReadOnlySettings settings;

    public SubscriptionInstaller(ReadOnlySettings settings)
    {
        this.settings = settings;
    }

    public async Task Install(string identity)
    {
        if (!settings.ShouldInstall<StorageType.Subscriptions>())
        {
            return;
        }
        var connectionBuilder = settings.GetConnectionBuilder<StorageType.Subscriptions>();

        var endpointName = settings.GetEndpointNamePrefix<StorageType.Subscriptions>();
        var createScript = Path.Combine(ScriptLocation.FindScriptDirectory(), "Subscription_Create.sql");
        log.Info($"Executing '{createScript}'");
        await SqlHelpers.Execute(connectionBuilder, File.ReadAllText(createScript),
            manipulateParameters: collection =>
            {
                collection.AddWithValue("schema", settings.GetSchema<StorageType.Subscriptions>());
                collection.AddWithValue("endpointName", endpointName);
            });
    }

}