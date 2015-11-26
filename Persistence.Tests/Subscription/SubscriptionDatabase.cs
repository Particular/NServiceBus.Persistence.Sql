using System;
using System.IO;
using System.Text;
using MethodTimer;
using NServiceBus.SqlPersistence;

class SubscriptionDatabase : IDisposable
{
    string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
    string endpointName = "Endpoint";
    [Time]
    public SubscriptionDatabase()
    {
        Drop();
        Create();

        Persister = new SubscriptionPersister(connectionString, "dbo", endpointName);
    }

    void Create()
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SubscriptionScriptBuilder.BuildCreateScript(writer);
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
            SubscriptionScriptBuilder.BuildDropScript(writer);
        }
        var script = builder.ToString();
        SqlHelpers.Execute(connectionString, script, collection =>
        {
            collection.AddWithValue("schema", "dbo");
            collection.AddWithValue("endpointName", endpointName);
        }).Await();
    }

    public SubscriptionPersister Persister;

    public void Dispose()
    {
    }
}