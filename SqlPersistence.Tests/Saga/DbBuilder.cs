using System.Data.SqlClient;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using SqlVarient = NServiceBus.Persistence.Sql.ScriptBuilder.SqlVarient;

static class SagaDbBuilder
{
    public static void ReCreate(string connection, string endpointName, params SagaDefinition[] sagaDefinitions)
    {
        Drop(connection, endpointName, sagaDefinitions);
        Create(connection, endpointName, sagaDefinitions);
    }

    static void Create(string connection, string endpointName, params SagaDefinition[] sagaDefinitions)
    {
        foreach (var sagaDefinition in sagaDefinitions)
        {
            Execute(connection, endpointName, SagaScriptBuilder.BuildCreateScript(sagaDefinition, SqlVarient.MsSqlServer));
        }
    }

    static void Drop(string connection, string endpointName, params SagaDefinition[] sagaDefinitions)
    {
        foreach (var sagaDefinition in sagaDefinitions)
        {
            Execute(connection, endpointName, SagaScriptBuilder.BuildDropScript(sagaDefinition, SqlVarient.MsSqlServer));
        }
    }

    static void Execute(string connection, string endpointName, string script)
    {
        using (var sqlConnection = new SqlConnection(connection))
        {
            sqlConnection.Open();
            using (var command = sqlConnection.CreateCommand())
            {
                command.CommandText = script;
                command.AddParameter("schema", "dbo");
                command.AddParameter("tablePrefix", $"{endpointName}.");
                command.ExecuteNonQuery();
            }
        }
    }
}