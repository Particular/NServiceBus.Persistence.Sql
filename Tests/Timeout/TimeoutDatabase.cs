using System;
using System.IO;
using System.Text;
using MethodTimer;
using NServiceBus.SqlPersistence;

class TimeoutDatabase : IDisposable
{
    string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
    string endpointName = "Endpoint";
    [Time]
    public TimeoutDatabase()
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            TimeoutScriptBuilder.BuildDrop("dbo", endpointName, writer);
            TimeoutScriptBuilder.BuildCreate("dbo", endpointName, writer);
        }
        var script = builder.ToString();
        SqlHelpers.Execute(connectionString, script);
        Persister = new TimeoutPersister(connectionString, "dbo", endpointName);
    }

    public TimeoutPersister Persister;

    public void Dispose()
    {
    }
}