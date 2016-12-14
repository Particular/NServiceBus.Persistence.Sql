using System;
using System.Data.Common;

static class SqlQueueDeletion
{
    static void DeleteQueue(DbConnection connection, string schema, string queueName)
    {
        var deleteScript = $@"
                    if exists (SELECT * from sys.objects where object_id = object_id(N'{schema}.{queueName}') and type in (N'U'))
                    drop table [{schema}].[{queueName}]";
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