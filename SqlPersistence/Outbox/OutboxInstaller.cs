using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Logging;
using NServiceBus.Persistence;
using NServiceBus.Settings;

class OutboxInstaller : INeedToInstallSomething
{
    ReadOnlySettings settings;
    static ILog log = LogManager.GetLogger<OutboxInstaller>();

    public OutboxInstaller(ReadOnlySettings settings)
    {
        this.settings = settings;
    }

    public async Task Install(string identity)
    {
        if (!settings.ShouldInstall<StorageType.Outbox>())
        {
            return;
        }
        var connectionBuilder = settings.GetConnectionBuilder<StorageType.Outbox>();

        var endpointName = settings.GetEndpointNamePrefix<StorageType.Outbox>();

        var createScript = Path.Combine(ScriptLocation.FindScriptDirectory(), "Outbox_Create.sql");
        log.Info($"Executing '{createScript}'");
        await SqlHelpers.Execute(connectionBuilder, File.ReadAllText(createScript),
            manipulateParameters: command =>
            {
                command.AddParameter("schema", settings.GetSchema<StorageType.Outbox>());
                command.AddParameter("endpointName", endpointName);
            });
    }

}