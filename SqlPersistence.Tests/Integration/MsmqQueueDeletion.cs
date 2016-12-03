using System;
using System.Messaging;

static class MsmqQueueDeletion
{

    public static void DeleteQueue(string queueName)
    {
        var path = $@"{Environment.MachineName}\private$\{queueName}";
        if (MessageQueue.Exists(path))
        {
            MessageQueue.Delete(path);
        }
    }

    public static void DeleteQueuesForEndpoint(string endpointName)
    {
        DeleteQueue(endpointName);
        DeleteQueue($"{endpointName}.retries");
        DeleteQueue($"{endpointName}.timeouts");
        DeleteQueue($"{endpointName}.timeoutsdispatcher");
    }
}