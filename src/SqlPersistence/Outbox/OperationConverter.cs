using DispatchProperties = NServiceBus.Transport.DispatchProperties;
using TransportOperation = NServiceBus.Outbox.TransportOperation;

static class OperationConverter
{
    public static SerializableOperation[] ToSerializable(this TransportOperation[] operations)
    {
        var result = new SerializableOperation[operations.Length];
        for (var i = 0; i < operations.Length; i++)
        {
            var operation = operations[i];
            result[i] = new SerializableOperation
            {
                Body = operation.Body,
                Headers = operation.Headers,
                MessageId = operation.MessageId,
                Options = operation.Options
            };
        }

        return result;
    }

    public static TransportOperation[] FromSerializable(this SerializableOperation[] operations)
    {
        var result = new TransportOperation[operations.Length];
        for (var i = 0; i < operations.Length; i++)
        {
            var operation = operations[i];
            result[i] = new TransportOperation(
                body: operation.Body,
                headers: operation.Headers,
                messageId: operation.MessageId,
                properties: new DispatchProperties(operation.Options));
        }

        return result;
    }
}