using System.Collections.Generic;
using System.Linq;
using NServiceBus.Transport;
using TransportOperation = NServiceBus.Outbox.TransportOperation;

static class OperationConverter
{
    public static IEnumerable<SerializableOperation> ToSerializable(this IEnumerable<TransportOperation> operations)
    {
        return operations.Select(
            operation => new SerializableOperation
            {
                Body = operation.Body,
                Headers = operation.Headers,
                MessageId = operation.MessageId,
                Options = operation.Options
            });
    }

    public static IEnumerable<TransportOperation> FromSerializable(this IEnumerable<SerializableOperation> operations)
    {
        return operations.Select(
            operation => new TransportOperation(
                body: operation.Body,
                headers: operation.Headers,
                messageId: operation.MessageId,
                properties: new DispatchProperties(operation.Options ?? new Dictionary<string, string>())
            ));
    }
}