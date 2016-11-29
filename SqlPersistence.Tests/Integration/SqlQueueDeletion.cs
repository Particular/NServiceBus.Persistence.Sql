using System;
using System.Data.Common;
using System.Threading.Tasks;

static class SqlQueueDeletion
{
    public static async Task DeleteQueue(DbConnection connection, string schema, string queueName)
    {
        var deleteScript = $@"
                    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{schema}].[{queueName}]') AND type in (N'U'))
                    DROP TABLE [{schema}].[{queueName}]";
        using (var command = connection.CreateCommand())
        {
            command.CommandText = deleteScript;
            await command.ExecuteNonQueryAsync();
        }
    }

    public static async Task DeleteQueuesForEndpoint(DbConnection connection, string schema, string endpointName)
    {
        //main queue
        await DeleteQueue(connection, schema, endpointName);

        //callback queue
        await DeleteQueue(connection, schema, $"{endpointName}.{Environment.MachineName}");

        //retries queue
        await DeleteQueue(connection, schema, $"{endpointName}.Retries");

        //timeout queue
        await DeleteQueue(connection, schema, $"{endpointName}.Timeouts");

        //timeout dispatcher queue
        await DeleteQueue(connection, schema, $"{endpointName}.TimeoutsDispatcher");
    }

}