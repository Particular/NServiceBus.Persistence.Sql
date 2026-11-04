using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;

partial class SagaPersister
{
    // Timeouts are stored as a JSON string value, rather than a nested object, so the metadata stays a
    // Dictionary<string, string> and versions without timeout management can still read and update the saga.
    const string TimeoutsMetadataKey = "Timeouts";

    static string BuildMetadata(IContainSagaData sagaData, IReadOnlyList<StoredSagaTimeout> timeouts)
    {
        var metadata = new Dictionary<string, string>();
        if (sagaData.OriginalMessageId != null)
        {
            metadata.Add("OriginalMessageId", sagaData.OriginalMessageId);
        }
        if (sagaData.Originator != null)
        {
            metadata.Add("Originator", sagaData.Originator);
        }
        if (timeouts.Count > 0)
        {
            metadata.Add(TimeoutsMetadataKey, SerializeTimeouts(timeouts));
        }
        return Serializer.Serialize(metadata);
    }

    static void ReadMetadata(DbDataReader dataReader, out string originator, out string originalMessageId, out IReadOnlyList<StoredSagaTimeout> timeouts)
    {
        using (var textReader = dataReader.GetTextReader(3))
        {
            var metadata = Serializer.Deserialize<Dictionary<string, string>>(textReader);
            metadata.TryGetValue("Originator", out originator);
            metadata.TryGetValue("OriginalMessageId", out originalMessageId);
            timeouts = metadata.TryGetValue(TimeoutsMetadataKey, out var serializedTimeouts)
                ? DeserializeTimeouts(serializedTimeouts)
                : [];
        }
    }

    static string SerializeTimeouts(IReadOnlyList<StoredSagaTimeout> timeouts)
    {
        using var writer = new StringWriter();
        TimeoutsSerializer.Serialize(writer, timeouts);
        return writer.ToString();
    }

    static List<StoredSagaTimeout> DeserializeTimeouts(string serializedTimeouts)
    {
        using var reader = new StringReader(serializedTimeouts);
        using var jsonReader = new JsonTextReader(reader);
        try
        {
            return TimeoutsSerializer.Deserialize<List<StoredSagaTimeout>>(jsonReader) ?? [];
        }
        catch (JsonException exception)
        {
            throw new SerializationException(exception);
        }
    }

    static readonly JsonSerializer TimeoutsSerializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        Formatting = Formatting.None,
        DateParseHandling = DateParseHandling.DateTimeOffset
    });
}
