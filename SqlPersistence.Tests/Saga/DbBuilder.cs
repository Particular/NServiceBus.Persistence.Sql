using System.Data.Common;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class SagaDbBuilder
{
    public static void ReCreate(DbConnection connection, string endpointName, params SagaDefinition[] sagaDefinitions)
    {
        Drop(connection, endpointName, sagaDefinitions);
        Create(connection, endpointName, sagaDefinitions);
    }

    static void Create(DbConnection connection, string endpointName, params SagaDefinition[] sagaDefinitions)
    {
        foreach (var sagaDefinition in sagaDefinitions)
        {
            Execute(connection, endpointName, SagaScriptBuilder.BuildCreateScript(sagaDefinition, SqlVarient.MsSqlServer));
        }
    }

    static void Drop(DbConnection connection, string endpointName, params SagaDefinition[] sagaDefinitions)
    {
        foreach (var sagaDefinition in sagaDefinitions)
        {
            Execute(connection, endpointName, SagaScriptBuilder.BuildDropScript(sagaDefinition, SqlVarient.MsSqlServer));
        }
    }

    static void Execute(DbConnection connection, string endpointName, string script)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = script;
            command.AddParameter("schema", "dbo");
            command.AddParameter("tablePrefix", $"{endpointName}.");
            command.ExecuteNonQuery();
        }
    }
}