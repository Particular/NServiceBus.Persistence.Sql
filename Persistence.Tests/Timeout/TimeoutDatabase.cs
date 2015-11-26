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
            TimeoutScriptBuilder.BuildCreateScript( writer);
        }
        var script = builder.ToString();
        SqlHelpers.Execute(connectionString, script, collection =>
        {
            collection.AddWithValue("schema", "dbo");
            collection.AddWithValue("endpointName", endpointName);
        }).Await();
    }

    void Drop()
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            TimeoutScriptBuilder.BuildDropScript(writer);
        }
        var script = builder.ToString();
        SqlHelpers.Execute(connectionString, script, collection =>
        {
            collection.AddWithValue("schema", "dbo");
            collection.AddWithValue("endpointName", endpointName);
        }).Await();
    }

    public TimeoutPersister Persister;

    public void Dispose()
    {
    }
}