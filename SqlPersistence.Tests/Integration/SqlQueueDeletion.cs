using System;
using System.Data.Common;

static class SqlQueueDeletion
{
    static void DeleteQueue(DbConnection connection, string schema, string queueName)
    {
        var deleteScript = $@"
                    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{schema}].[{queueName}]') AND type in (N'U'))
                    DROP TABLE [{schema}].[{queueName}]";
        using (var command = connection.CreateCommand())
        {
            command.CommandText = deleteScript;
            command.ExecuteNonQuery();
        }
    }

    public static void DeleteQueuesForEndpoint(DbConnection connection, string schema, string endpointName)
    {
        DeleteQueue(connection, schema, endpointName);
        DeleteQueue(connection, schema, $"{endpointName}.{Environment.MachineName}");
        DeleteQueue(connection, schema, $"{endpointName}.Retries");
        DeleteQueue(connection, schema, $"{endpointName}.Timeouts");
        DeleteQueue(connection, schema, $"{endpointName}.TimeoutsDispatcher");
    }

}