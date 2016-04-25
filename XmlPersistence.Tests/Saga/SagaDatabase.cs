using System;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using MethodTimer;
using NServiceBus.Persistence.SqlServerXml;

class SagaDatabase : IDisposable
{
    string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=SqlPersistenceTests;Integrated Security=True";
    string endpointName = "Endpoint";

    [Time]
    public SagaDatabase(SagaDefinition sagaDefinition)
    {
        SqlConnection = new SqlConnection(connectionString);
        SqlConnection.Open();
        SqlTransaction = SqlConnection.BeginTransaction();
        Drop(sagaDefinition);
        Create(sagaDefinition);
        var commandBuilder = new SagaCommandBuilder("dbo",endpointName);
        var infoCache = new SagaInfoCache(null,null,commandBuilder,(serializer, type) => {});
        Persister = new SagaPersister(infoCache);
    }

    public SqlTransaction SqlTransaction;

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
        }).Await();
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
        }).Await();
    }

    public SagaPersister Persister;
    public SqlConnection SqlConnection;

    public void Dispose()
    {
    }
}