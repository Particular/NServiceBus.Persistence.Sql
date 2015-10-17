using System;
using System.IO;
using System.Text;
using MethodTimer;
using NServiceBus.SqlPersistence;

class SagaDatabase : IDisposable
{
    string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
    string endpointName = "Endpoint";

    [Time]
    public SagaDatabase(SagaDefinition sagaDefinition)
    {
        Drop(sagaDefinition);
        Create(sagaDefinition);
        var commandBuilder = new SagaCommandBuilder("dbo",endpointName);
        var infoCache = new SagaInfoCache(null,null,commandBuilder,(serializer, type) => {});
        Persister = new SagaPersister(connectionString,infoCache);
    }

    void Create(SagaDefinition sagaDefinition)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SagaScriptBuilder.BuildCreateScript(sagaDefinition, writer);
        }
        var script = builder.ToString();
        SqlHelpers.Execute(connectionString, script, collection =>
        {
            collection.AddWithValue("schema", "dbo");
            collection.AddWithValue("endpointName", endpointName);
        });
    }

    void Drop(SagaDefinition sagaDefinition)
    {
        var builder = new StringBuilder();

        using (var writer = new StringWriter(builder))
        {
            SagaScriptBuilder.BuildDropScript(sagaDefinition.Name, writer);
        }
        var script = builder.ToString();
        SqlHelpers.Execute(connectionString, script, collection =>
        {
            collection.AddWithValue("schema", "dbo");
            collection.AddWithValue("endpointName", endpointName);
        });
    }

    public SagaPersister Persister;

    public void Dispose()
    {
    }
}