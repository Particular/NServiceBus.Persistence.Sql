using System;
using System.IO;
using System.Text;
using ApprovalTests;
using ApprovalTests.Namers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NUnit.Framework;

static class TestApprover
{
    public static JsonSerializer JsonSerializer { get; set; }

    static TestApprover()
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };
        settings.Converters.Add(new StringEnumConverter());
        JsonSerializer = JsonSerializer.Create(settings);
    }

    public static void Verify(string text)
    {
        var writer = new ApprovalTextWriter(text);
        var namer = new ApprovalNamer();
        Approvals.Verify(writer, namer, Approvals.GetReporter());
    }

    public static void VerifyWithJson(object target, Func<string, string> scrubber = null, JsonSerializerSettings jsonSerializerSettings = null)
    {
        var formatJson = AsFormattedJson(target, jsonSerializerSettings);
        if (scrubber == null)
        {
            scrubber = s => s;
        }

        Verify(scrubber(formatJson));
    }

    public static string AsFormattedJson(object target, JsonSerializerSettings jsonSerializerSettings = null)
    {
        var builder = new StringBuilder();
        using (var stringWriter = new StringWriter(builder))
        {
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                var jsonSerializer = GetJsonSerializer(jsonSerializerSettings);

                jsonWriter.Formatting = jsonSerializer.Formatting;
                jsonSerializer.Serialize(jsonWriter, target);
            }

            return stringWriter.ToString();
        }
    }

    static JsonSerializer GetJsonSerializer(JsonSerializerSettings jsonSerializerSettings)
    {
        if (jsonSerializerSettings == null)
        {
            return JsonSerializer;
        }

        return JsonSerializer.Create(jsonSerializerSettings);
    }

    class ApprovalNamer : UnitTestFrameworkNamer
    {
        public override string SourcePath { get; } = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "ApprovalFiles");
    }
}