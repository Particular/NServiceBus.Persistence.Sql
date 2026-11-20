using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using NServiceBus.Outbox;
using NUnit.Framework;
using DispatchProperties = NServiceBus.Transport.DispatchProperties;

[TestFixture]
public class OutboxSerializationTests
{
    [TestCase(200)]
    [TestCase(1024 * 1024)]
    public void MatchesStringWriterOutput(int bodySize)
    {
        var body = new byte[bodySize];
        new Random(42).NextBytes(body);
        var operations = new List<TransportOperation>
        {
            new(
                messageId: "Id1",
                properties: new DispatchProperties(new Dictionary<string, string> { { "Option", "Value" } }),
                body: body,
                headers: new Dictionary<string, string>
                {
                    { "NServiceBus.EnclosedMessageTypes", "MyMessage, MyAssembly" },
                    { "Quoted", "\"value\" with \\ and\nnew line" },
                    { "Unicode", "äöü" }
                })
        };
        var serializable = operations.ToSerializable();

        var json = Serializer.Serialize(serializable);

        Assert.That(json, Is.EqualTo(SerializeWithStringWriter(serializable)));
    }

    static string SerializeWithStringWriter(object target)
    {
        var builder = new StringBuilder();
        using (var jsonWriter = new JsonTextWriter(new StringWriter(builder)))
        {
            Serializer.JsonSerializer.Serialize(jsonWriter, target);
        }

        return builder.ToString();
    }
}
