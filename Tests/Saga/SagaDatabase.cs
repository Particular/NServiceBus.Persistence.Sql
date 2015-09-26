using System;
using System.Collections.Generic;
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
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            var sagaDefinitions = new List<SagaDefinition> {sagaDefinition};
            var sagaNames = new List<string> {sagaDefinition.Name};
            SagaScriptBuilder.BuildDropScript("dbo", endpointName, sagaNames, s => writer);
            SagaScriptBuilder.BuildCreateScript("dbo", endpointName, sagaDefinitions, s => writer);
        }
        var script = builder.ToString();
        SqlHelpers.Execute(connectionString, script);
        Persister = new SagaPersister(connectionString, "dbo", endpointName);
    }

    public SagaPersister Persister;

    public void Dispose()
    {
    }
}