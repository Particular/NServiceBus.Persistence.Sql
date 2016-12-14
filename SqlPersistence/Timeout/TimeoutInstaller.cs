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

        var sqlVarient = settings.GetSqlVarient();
        var tablePrefix = settings.GetTablePrefix<StorageType.Timeouts>();

        var createScript = Path.Combine(ScriptLocation.FindScriptDirectory(sqlVarient), "Timeout_Create.sql");
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