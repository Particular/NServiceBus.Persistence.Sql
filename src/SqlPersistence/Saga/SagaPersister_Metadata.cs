using System.Collections.Generic;
using System.Data.Common;
using NServiceBus;

partial class SagaPersister
{
    static string BuildMetadata(IContainSagaData sagaData)
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
        return Serializer.Serialize(metadata);
    }

    static void ReadMetadata(DbDataReader dataReader, out string originator, out string originalMessageId)
    {
        using (var textReader = dataReader.GetTextReader(3))
        {
            var metadata = Serializer.Deserialize<Dictionary<string, string>>(textReader);
            metadata.TryGetValue("Originator", out originator);
            metadata.TryGetValue("OriginalMessageId", out originalMessageId);
        }
    }
}
