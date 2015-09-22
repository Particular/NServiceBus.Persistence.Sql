using System.Data.SqlClient;
using NServiceBus;
using NServiceBus.Installation;
using NServiceBus.SqlPersistence;

class TimeoutInstaller : INeedToInstallSomething
{
    
    public void Install(string identity, Configure config)
    {
        var connectionString = config.Settings.GetConnectionString();
        var script = TimeoutScriptBuilder.Build("dbo", config.Settings.EndpointName());
        using (var sqlConnection = OpenSqlConnection.New(connectionString))
        using (var command = new SqlCommand(script, sqlConnection))
        {
            command.ExecuteNonQuery();
        }
    }
}