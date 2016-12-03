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

    public Task Install(string identity)
    {
        if (!settings.ShouldInstall<StorageType.Outbox>())
        {
            return Task.FromResult(0);
        }
        var connectionBuilder = settings.GetConnectionBuilder<StorageType.Outbox>();

        var sqlVarient = settings.GetSqlVarient();
        var endpointName = settings.GetEndpointNamePrefix<StorageType.Outbox>();

        var createScript = Path.Combine(ScriptLocation.FindScriptDirectory(sqlVarient), "Outbox_Create.sql");
        log.Info($"Executing '{createScript}'");
        return SqlHelpers.Execute(connectionBuilder, File.ReadAllText(createScript),
            manipulateCommand: command =>
            {
                command.AddParameter("schema", settings.GetSchema<StorageType.Outbox>());
                command.AddParameter("endpointName", endpointName);
            });
    }

}