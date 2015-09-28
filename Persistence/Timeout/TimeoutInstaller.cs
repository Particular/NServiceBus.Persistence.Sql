using System.IO;
using System.Text;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.Persistence;
using NServiceBus.SqlPersistence;

class TimeoutInstaller : INeedToInstallSomething
{
    
    public void Install(string identity, Configure config)
    {
        var settings = config.Settings;
        if (!settings.GetFeatureEnabled<StorageType.Subscriptions>())
        {
            return;
        }
        if (settings.GetDisableInstaller<StorageType.Timeouts>())
        {
            return;
        }
        var connectionString = settings.GetConnectionString<StorageType.Timeouts>();
        var endpointName = settings.EndpointName();
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            TimeoutScriptBuilder.BuildCreateScript("dbo", endpointName, writer);
        }

        SqlHelpers.Execute(connectionString, builder.ToString());
    }
}