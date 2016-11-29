using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Logging;
using NServiceBus.Persistence;
using NServiceBus.Settings;

class TimeoutInstaller : INeedToInstallSomething
{
    static ILog log = LogManager.GetLogger<TimeoutInstaller>();
    ReadOnlySettings settings;

    public TimeoutInstaller(ReadOnlySettings settings)
    {
        this.settings = settings;
    }

    public async Task Install(string identity)
    {
        if (!settings.ShouldInstall<StorageType.Subscriptions>())
        {
            return;
        }
        var connectionBuilder = settings.GetConnectionBuilder<StorageType.Timeouts>();

        var endpointName = settings.GetEndpointNamePrefix<StorageType.Timeouts>();

        var createScript = Path.Combine(ScriptLocation.FindScriptDirectory(), "Timeout_Create.sql");
        log.Info($"Executing '{createScript}'");
        await SqlHelpers.Execute(connectionBuilder, File.ReadAllText(createScript),
            manipulateParameters: collection =>
            {
                collection.AddWithValue("schema", settings.GetSchema<StorageType.Timeouts>());
                collection.AddWithValue("endpointName", endpointName);
            });
    }

}