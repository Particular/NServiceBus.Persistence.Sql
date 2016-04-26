using System;
using System.Messaging;

public static class QueueDeletion
{

    public static void DeleteAllQueues()
    {
        var machineQueues = MessageQueue.GetPrivateQueuesByMachine(".");
        foreach (var q in machineQueues)
        {
            MessageQueue.Delete(q.Path);
        }
    }

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
        //main queue
        DeleteQueue(endpointName);

        //retries queue
        DeleteQueue(endpointName + ".retries");

        //timeout queue
        DeleteQueue(endpointName + ".timeouts");

        //timeout dispatcher queue
        DeleteQueue(endpointName + ".timeoutsdispatcher");
    }
}