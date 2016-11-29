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
            manipulateParameters: command =>
            {
                command.AddParameter("schema", settings.GetSchema<StorageType.Timeouts>());
                command.AddParameter("endpointName", endpointName);
            });
    }

}