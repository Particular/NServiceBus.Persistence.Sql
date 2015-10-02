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
        Drop();
        Create();
        Persister = new TimeoutPersister(connectionString, "dbo",endpointName);
    }

    void Create()
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            TimeoutScriptBuilder.BuildCreateScript("dbo", endpointName, writer);
        }
        var script = builder.ToString();
        SqlHelpers.Execute(connectionString, script);
    }

    void Drop()
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            TimeoutScriptBuilder.BuildDropScript("dbo", endpointName, writer);
        }
        var script = builder.ToString();
        SqlHelpers.Execute(connectionString, script);
    }

    public TimeoutPersister Persister;

    public void Dispose()
    {
    }
}