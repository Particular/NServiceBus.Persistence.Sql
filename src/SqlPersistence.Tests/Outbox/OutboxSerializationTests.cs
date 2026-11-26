using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NServiceBus.Outbox;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;
using DispatchProperties = NServiceBus.Transport.DispatchProperties;

[TestFixture]
public class OutboxSerializationTests
{
    // Written by 9.0.x (Newtonsoft, indented) so the test does not depend on the Newtonsoft settings in this repository.
    const string Golden9x = """
        [
          {
            "Body": "ICE=",
            "Headers": {
              "Quote": "\"q\" \\ \n",
              "Special": "+ ` <>&'",
              "Unicode": "Zürich 😀",
              "Null": null,
              "Empty": ""
            },
            "MessageId": "Id1",
            "Options": {
              "OptionKey1": "OptionValue1"
            }
          },
          {}
        ]
        """;

    [Test]
    public void ReadsTextWrittenByVersion9()
    {
        var operations = OutboxSerializer.Deserialize(new StringReader(Golden9x));

        Assert.That(operations, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(operations[0].Body.ToArray(), Is.EqualTo(new byte[] { 0x20, 0x21 }));
            Assert.That(operations[0].Headers, Is.EqualTo(new Dictionary<string, string>
            {
                ["Quote"] = "\"q\" \\ \n",
                ["Special"] = "+ ` <>&'",
                ["Unicode"] = "Zürich 😀",
                ["Null"] = null,
                ["Empty"] = ""
            }));
            Assert.That(operations[0].MessageId, Is.EqualTo("Id1"));
            Assert.That(operations[0].Options, Is.EqualTo(new Dictionary<string, string> { ["OptionKey1"] = "OptionValue1" }));
            Assert.That(operations[1].Body.IsEmpty, Is.True);
            Assert.That(operations[1].Headers, Is.Null);
            Assert.That(operations[1].MessageId, Is.Null);
            Assert.That(operations[1].Options, Is.Null);
        });
    }

    [TestCaseSource(nameof(CaseNames))]
    public void VersionNineWritesAndNewReads(string caseName)
    {
        var operations = CaseData[caseName];
        var json = Serializer.Serialize(operations);

        var result = OutboxSerializer.Deserialize(new StringReader(json));

        AssertEqual(operations, result);
    }

    [TestCaseSource(nameof(CaseNames))]
    public void NewWritesAndVersionNineReads(string caseName)
    {
        var operations = CaseData[caseName];
        var json = OutboxSerializer.Serialize(operations);

        var result = Serializer.Deserialize<IEnumerable<SerializableOperation>>(new StringReader(json)).ToArray();

        AssertEqual(operations, result);
    }

    [TestCaseSource(nameof(CaseNames))]
    public void NewWritesAndNewReads(string caseName)
    {
        var operations = CaseData[caseName];
        var json = OutboxSerializer.Serialize(operations);

        var result = OutboxSerializer.Deserialize(new StringReader(json));

        AssertEqual(operations, result);
    }

    [TestCaseSource(nameof(CaseNames))]
    public void WritesTheSameTextAsTheSerializerWithEquivalentOptions(string caseName)
    {
        var operations = CaseData[caseName];
        var typeInfo = OutboxJsonContext.Create().SerializableOperationArray;

        var json = OutboxSerializer.Serialize(operations);

        Assert.That(json, Is.EqualTo(JsonSerializer.Serialize(operations, typeInfo)));
    }

    [Test]
    public void WritesCompactTextWithoutByteOrderMark()
    {
        var json = OutboxSerializer.Serialize([new SerializableOperation { Body = new byte[] { 0x20, 0x21 }, Headers = new Dictionary<string, string> { ["k"] = "v" }, MessageId = "Id1" }]);

        Assert.That(json, Is.EqualTo("""[{"Body":"ICE=","Headers":{"k":"v"},"MessageId":"Id1"}]"""));
    }

    [Test]
    public void OmitsNullsAndDefaultBodyButWritesEmptyBody()
    {
        var json = OutboxSerializer.Serialize([new SerializableOperation(), new SerializableOperation { Body = new byte[0] }]);

        Assert.That(json, Is.EqualTo("""[{},{"Body":""}]"""));
    }

    [Test]
    public void KeepsNonAsciiLettersUnescapedAndHtmlSensitiveCharactersEscaped()
    {
        var json = OutboxSerializer.Serialize([new SerializableOperation { Headers = new Dictionary<string, string> { ["k"] = "Zürich <&>" } }]);

        Assert.That(json, Is.EqualTo("""[{"Headers":{"k":"Zürich \u003C\u0026\u003E"}}]"""));
    }

    [Test]
    public void DelayOptionsSurviveAnExchangeBetweenVersions()
    {
        var delayDeliveryWith = TimeSpan.FromMinutes(5);
        var doNotDeliverBefore = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));
        var discardIfNotReceivedBefore = TimeSpan.FromHours(1);
        var properties = new DispatchProperties
        {
            DelayDeliveryWith = new NServiceBus.DelayedDelivery.DelayDeliveryWith(delayDeliveryWith),
            DoNotDeliverBefore = new NServiceBus.DelayedDelivery.DoNotDeliverBefore(doNotDeliverBefore),
            DiscardIfNotReceivedBefore = new NServiceBus.Performance.TimeToBeReceived.DiscardIfNotReceivedBefore(discardIfNotReceivedBefore)
        };
        var operations = new[] { new TransportOperation("Id1", properties, new byte[] { 1 }, []) }.ToSerializable();

        var readByNew = OutboxSerializer.Deserialize(new StringReader(Serializer.Serialize(operations))).FromSerializable();
        var readByVersionNine = Serializer.Deserialize<IEnumerable<SerializableOperation>>(new StringReader(OutboxSerializer.Serialize(operations))).ToArray().FromSerializable();

        foreach (var result in new[] { readByNew, readByVersionNine })
        {
            var resultProperties = new DispatchProperties(result[0].Options);
            Assert.Multiple(() =>
            {
                Assert.That(resultProperties.DelayDeliveryWith.Delay, Is.EqualTo(delayDeliveryWith));
                Assert.That(resultProperties.DoNotDeliverBefore.At, Is.EqualTo(doNotDeliverBefore));
                Assert.That(resultProperties.DiscardIfNotReceivedBefore.MaxTime, Is.EqualTo(discardIfNotReceivedBefore));
            });
        }
    }

    [Test]
    public void ReadsRecordsPaddedWithTrailingSpaces()
    {
        var json = OutboxSerializer.Serialize([new SerializableOperation { Body = new byte[] { 1, 2, 3 }, MessageId = "Id1" }]);
        var padded = json.PadRight(4001, ' ');

        var result = OutboxSerializer.Deserialize(new StringReader(padded));

        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result[0].MessageId, Is.EqualTo("Id1"));
    }

    [TestCase(1)]
    [TestCase(7)]
    public void ReadsFromReadersThatReturnFewCharactersPerRead(int charactersPerRead)
    {
        var operations = new[] { new SerializableOperation { Body = new byte[10_000], MessageId = "Id1", Headers = new Dictionary<string, string> { ["k"] = "Zürich" } } };
        var json = OutboxSerializer.Serialize(operations);

        var result = OutboxSerializer.Deserialize(new TrickleReader(json, charactersPerRead));

        AssertEqual(operations, result);
    }

    [TestCase("""[{"Body":"ICE=" """)]
    [TestCase("""[{"Body":"not base64!"}]""")]
    [TestCase("""[{"Headers":{"k":1}}]""")]
    [TestCase("""[1]""")]
    [TestCase("")]
    [TestCase("{}")]
    [TestCase("\"text\"")]
    [TestCase("null")]
    [TestCase("﻿[]")]
    public void InvalidDocumentsThrowSerializationException(string json)
    {
        Assert.Throws<SerializationException>(() => OutboxSerializer.Deserialize(new StringReader(json)));
    }

    [Test]
    public void ReaderFailuresThrowSerializationException()
    {
        var exception = Assert.Throws<SerializationException>(() => OutboxSerializer.Deserialize(new ThrowingReader()));

        Assert.That(exception.InnerException, Is.TypeOf<IOException>());
    }

    [Test]
    public void LoneSurrogatesAreWrittenAsReplacementCharacters()
    {
        var operations = new[] { new SerializableOperation { Headers = new Dictionary<string, string> { ["k"] = "a\ud800b" } } };

        var json = OutboxSerializer.Serialize(operations);
        TestContext.Out.WriteLine(json);
        var result = OutboxSerializer.Deserialize(new StringReader(json));

        Assert.That(json, Is.EqualTo("""[{"Headers":{"k":"a\uFFFDb"}}]"""));
        Assert.That(result[0].Headers["k"], Is.EqualTo("a\ufffdb"));
    }

    static IEnumerable<string> CaseNames => CaseData.Keys;

    static readonly Dictionary<string, SerializableOperation[]> CaseData = CreateCases();

    static Dictionary<string, SerializableOperation[]> CreateCases()
    {
        var cases = new Dictionary<string, SerializableOperation[]>();
        var random = new Random(42);
        var body2K = new byte[2 * 1024];
        random.NextBytes(body2K);
        var body1M = new byte[1024 * 1024];
        random.NextBytes(body1M);
        var body64K = new byte[64 * 1024];
        random.NextBytes(body64K);

        Add(cases, "zero operations", []);
        Add(cases, "default operation", new SerializableOperation());
        Add(cases, "empty body", new SerializableOperation { Body = new byte[0], MessageId = "Id1" });
        Add(cases, "2 KB body", new SerializableOperation { Body = body2K, MessageId = "Id1", Headers = new Dictionary<string, string> { ["NServiceBus.EnclosedMessageTypes"] = "MyMessage, MyAssembly" } });
        Add(cases, "1 MB body", new SerializableOperation { Body = body1M, MessageId = "Id1" });
        Add(cases, "null headers", new SerializableOperation { Body = new byte[] { 1 }, MessageId = "Id1", Options = new Dictionary<string, string> { ["o"] = "v" } });
        Add(cases, "null options", new SerializableOperation { Body = new byte[] { 1 }, MessageId = "Id1", Headers = new Dictionary<string, string> { ["h"] = "v" } });
        Add(cases, "null message id", new SerializableOperation { Body = new byte[] { 1 }, Headers = [], Options = [] });
        Add(cases, "empty message id", new SerializableOperation { Body = new byte[] { 1 }, MessageId = "" });
        Add(cases, "null and empty header values", new SerializableOperation { Headers = new Dictionary<string, string> { ["null"] = null, ["empty"] = "" } });
        Add(cases, "special characters", new SerializableOperation
        {
            Headers = new Dictionary<string, string>
            {
                ["quote"] = "\"value\" with quote",
                ["backslash"] = "a\\b",
                ["newline"] = "a\nb\r\nc\td",
                ["plus"] = "a+b",
                ["backtick"] = "a`b",
                ["html"] = "<a href='x'>&amp;</a>",
                ["unicode"] = "Zürich äöü 日本語",
                ["emoji"] = "😀 family 👨‍👩‍👧",
                ["control"] = "a\u0001b\u001Fc"
            },
            Options = new Dictionary<string, string> { ["quote\"key"] = "ü" }
        });
        Add(cases, "multiple operations",
            new SerializableOperation { Body = body64K, MessageId = "Id1", Headers = new Dictionary<string, string> { ["h"] = "1" } },
            new SerializableOperation { Body = body64K, MessageId = "Id2", Options = new Dictionary<string, string> { ["o"] = "2" } },
            new SerializableOperation { Body = body64K, MessageId = "Id3" });

        return cases;
    }

    static void Add(Dictionary<string, SerializableOperation[]> cases, string name, params SerializableOperation[] operations) => cases.Add(name, operations);


    static void AssertEqual(SerializableOperation[] expected, SerializableOperation[] actual)
    {
        Assert.That(actual, Has.Length.EqualTo(expected.Length));
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(actual[i].Body.ToArray(), Is.EqualTo(expected[i].Body.ToArray()), "Body");
                Assert.That(actual[i].Headers, Is.EqualTo(expected[i].Headers), "Headers");
                Assert.That(actual[i].Options, Is.EqualTo(expected[i].Options), "Options");
                Assert.That(actual[i].MessageId, Is.EqualTo(expected[i].MessageId), "MessageId");
            });
        }
    }

    sealed class TrickleReader(string text, int charactersPerRead) : TextReader
    {
        int position;

        public override int Read(char[] buffer, int index, int count)
        {
            var length = Math.Min(Math.Min(count, charactersPerRead), text.Length - position);
            text.CopyTo(position, buffer, index, length);
            position += length;
            return length;
        }
    }

    sealed class ThrowingReader : TextReader
    {
        public override int Read(char[] buffer, int index, int count) => throw new IOException("Connection lost");
    }
}