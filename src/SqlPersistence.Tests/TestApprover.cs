using System;
using System.IO;
using ApprovalTests;
using ApprovalTests.Namers;
using Newtonsoft.Json;
using NUnit.Framework;
using ObjectApproval;

static class TestApprover
{
    static TestApprover()
    {
        ObjectApprover.JsonSerializer.DefaultValueHandling = DefaultValueHandling.Include;
    }

    public static void Verify(string text)
    {
        var writer = new ApprovalTextWriter(text);
        var namer = new ApprovalNamer();
        Approvals.Verify(writer, namer, Approvals.GetReporter());
    }

    public static void VerifyWithJson(object target, Func<string, string> scrubber = null, JsonSerializerSettings jsonSerializerSettings = null)
    {
        if (scrubber == null)
        {
            scrubber = s => s;
        }

        var json = ObjectApprover.AsFormattedJson(target, jsonSerializerSettings);
        Verify(scrubber(json));
    }

    class ApprovalNamer : UnitTestFrameworkNamer
    {
        public override string SourcePath { get; } = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "ApprovalFiles");
    }
}