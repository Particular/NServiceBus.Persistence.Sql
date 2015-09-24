using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.SqlPersistence;

class TimeoutInstaller : INeedToInstallSomething
{
    
    public void Install(string identity, Configure config)
    {
        var settings = config.Settings;
        if (!settings.IsTimeoutEnabled())
        {
            return;
        }
        var connectionString = settings.GetConnectionString();
        var endpointName = settings.EndpointName();
        Install(endpointName, connectionString);
    }

    internal static void Install(string endpointName, string connectionString)
    {
        var script = TimeoutScriptBuilder.BuildCreate("dbo", endpointName);
        SqlHelpers.Execute(connectionString, script);
    }
    internal static void Drop(string endpointName, string connectionString)
    {
        var script = TimeoutScriptBuilder.BuildDrop("dbo", endpointName);
        SqlHelpers.Execute(connectionString,script);
    }


}