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
        var connectionString = settings.GetConnectionString<StorageType.Timeouts>();
        var endpointName = settings.EndpointName().ToString();
        var createScript = Path.Combine(ScriptLocation.FindScriptDirectory(), "Timeout_Create.sql");
        log.Info($"Executing '{createScript}'");
        await SqlHelpers.Execute(connectionString, File.ReadAllText(createScript), collection =>
        {
            collection.AddWithValue("schema", settings.GetSchema<StorageType.Timeouts>());
            collection.AddWithValue("endpointName", endpointName);
        });
    }

}