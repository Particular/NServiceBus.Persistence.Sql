using System.IO;
using System.Text.Json.Serialization.Metadata;
using NServiceBus.Persistence.Sql;

static class OutboxSerializer
{
    static readonly JsonTypeInfo<SerializableOperation[]> TypeInfo = OutboxJsonContext.Create().SerializableOperationArray;

    public static string Serialize(SerializableOperation[] operations) =>
        JsonPersistenceSerializer.Serialize(operations, TypeInfo);

    public static SerializableOperation[] Deserialize(TextReader reader) =>
        JsonPersistenceSerializer.Deserialize(reader, TypeInfo) ?? throw new SerializationException("The outbox operations document is null.");
}