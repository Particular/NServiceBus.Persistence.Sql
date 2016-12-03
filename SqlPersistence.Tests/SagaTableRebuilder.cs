using System;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using SqlVarient = NServiceBus.Persistence.Sql.ScriptBuilder.SqlVarient;

static class SagaTableRebuilder
{
    public static async Task ReCreate(Func<Task<DbConnection>> connectionBuilder, SagaDefinition sagaDefinition, SqlVarient sqlVarient)
    {
        await Execute(connectionBuilder, TimeoutScriptBuilder.BuildDropScript(sqlVarient));
        await Execute(connectionBuilder, TimeoutScriptBuilder.BuildCreateScript(sqlVarient));
        await Execute(connectionBuilder, SagaScriptBuilder.BuildDropScript(sagaDefinition, sqlVarient));
        await Execute(connectionBuilder, SagaScriptBuilder.BuildCreateScript(sagaDefinition, sqlVarient));
    }

    static async Task Execute(Func<Task<DbConnection>> connectionBuilder, string script)
    {
        using (var connection = await connectionBuilder())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = script;
            command.AddParameter("schema", "dbo");
            command.AddParameter("endpointName", "endpointName");
            await command.ExecuteNonQueryAsync();
        }
    }
}