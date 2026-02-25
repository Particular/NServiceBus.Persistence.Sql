namespace AnalyzerTests;

using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[SetUpFixture]
public class OneTimeSetUp
{
    [OneTimeSetUp]
    public void Setup() => SourceGeneratorTest.ConfigureAllSourceGeneratorTests(test => test.AddReferences(References));

    static readonly MetadataReference[] References =
    [
        MetadataReference.CreateFromFile(typeof(IMessage).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(EndpointConfiguration).Assembly.Location)
    ];
}