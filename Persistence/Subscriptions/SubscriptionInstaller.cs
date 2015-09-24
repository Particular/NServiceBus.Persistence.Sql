using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.SqlPersistence;

class SubscriptionInstaller : INeedToInstallSomething
{

    public void Install(string identity, Configure config)
    {
        var settings = config.Settings;
        if (!settings.IsSubscriptionEnabled())
        {
            return;
        }
        var connectionString = settings.GetConnectionString();
        var endpointName = settings.EndpointName();
        Install(endpointName, connectionString);
    }

    internal static void Install(string endpointName, string connectionString)
    {
        var script = SubscriptionScriptBuilder.BuildCreate("dbo", endpointName);
        SqlHelpers.Execute(connectionString, script);
    }

    internal static void Drop(string endpointName, string connectionString)
    {
        var script = SubscriptionScriptBuilder.BuildDrop("dbo", endpointName);
        SqlHelpers.Execute(connectionString, script);
    }
}