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
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SubscriptionScriptBuilder.BuildDropScript("dbo", endpointName, writer);
            SubscriptionScriptBuilder.BuildCreateScript("dbo", endpointName, writer);
        }
        var script = builder.ToString();
        SqlHelpers.Execute(connectionString, script);

        Persister = new SubscriptionPersister(connectionString, "dbo", endpointName);
    }

    public SubscriptionPersister Persister;

    public void Dispose()
    {
    }
}