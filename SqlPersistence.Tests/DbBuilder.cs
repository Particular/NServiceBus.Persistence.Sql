using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Persistence.Sql;

static class DbBuilder
{
    public static async Task ReCreate(string connection, string endpointName, params SagaDefinition[] sagaDefinitions)
    {
        await Drop(connection, endpointName, sagaDefinitions);
        await Create(connection, endpointName, sagaDefinitions);
    }

    static async Task Create(string connection, string endpointName, params SagaDefinition[] sagaDefinitions)
    {
        foreach (var sagaDefinition in sagaDefinitions)
        {
            await
                Execute(connection, endpointName, writer => SagaScriptBuilder.BuildCreateScript(sagaDefinition, writer));
        }
        await Execute(connection, endpointName, SubscriptionScriptBuilder.BuildCreateScript);
        await Execute(connection, endpointName, OutboxScriptBuilder.BuildCreateScript);
        await Execute(connection, endpointName, TimeoutScriptBuilder.BuildCreateScript);
    }

    static async Task Drop(string connection, string endpointName, params SagaDefinition[] sagaDefinitions)
    {
        foreach (var sagaDefinition in sagaDefinitions)
        {
            await Execute(connection, endpointName, writer => SagaScriptBuilder.BuildDropScript(sagaDefinition, writer));
        }
        await Execute(connection, endpointName, SubscriptionScriptBuilder.BuildDropScript);
        await Execute(connection, endpointName, OutboxScriptBuilder.BuildDropScript);
        await Execute(connection, endpointName, TimeoutScriptBuilder.BuildDropScript);
    }

    static Task Execute(string connection, string endpointName, Action<TextWriter> action)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            action(writer);
        }
        var script = builder.ToString();
        return SqlHelpers.Execute(connection, script,
            manipulateParameters: collection =>
            {
                collection.AddWithValue("schema", "dbo");
                collection.AddWithValue("endpointName", $"{endpointName}.");
            });
    }
}