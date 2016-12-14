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

        var sqlVarient = settings.GetSqlVarient();
        var tablePrefix = settings.GetTablePrefix<StorageType.Outbox>();

        var createScript = Path.Combine(ScriptLocation.FindScriptDirectory(sqlVarient), "Outbox_Create.sql");
        log.Info($"Executing '{createScript}'");
        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync().ConfigureAwait(false);
            await connection.ExecuteTableCommand(
                script: File.ReadAllText(createScript),
                tablePrefix: tablePrefix);
        }
    }

}