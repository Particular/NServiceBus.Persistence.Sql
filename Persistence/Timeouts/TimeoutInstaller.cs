using System.Data.SqlClient;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.SqlPersistence;

class TimeoutInstaller : INeedToInstallSomething
{
    
    public void Install(string identity, Configure config)
    {
        var connectionString = config.Settings.GetConnectionString();
        var endpointName = config.Settings.EndpointName();
        Install(endpointName, connectionString);
    }

    internal static void Install(string endpointName, string connectionString)
    {
        var script = TimeoutScriptBuilder.BuildCreate("dbo", endpointName);
        using (var sqlConnection = OpenSqlConnection.New(connectionString))
        using (var command = new SqlCommand(script, sqlConnection))
        {
            command.ExecuteNonQuery();
        }
    }
}