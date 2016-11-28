using System.IO;
using System.Text;
using ApprovalTests;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;

[TestFixture]
public class SagaScriptBuilderTest
{
    [Test]
    public void CreateWithCorrelation()
    {
        var saga = new SagaDefinition(
            name: "theSaga",
            tableSuffix: "theSaga",
            correlationProperty: new CorrelationProperty
            {
                Name = "CorrelationProperty",
                Type = CorrelationMemberType.String
            }
        );

        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SagaScriptBuilder.BuildCreateScript(saga, writer);
        }
        var script = builder.ToString();

        SqlValidator.Validate(script);
        Approvals.Verify(script);
    }

    [Test]
    public void CreateWithNoCorrelation()
    {
        var saga = new SagaDefinition(
            tableSuffix: "theSaga",
            name: "theSaga"
        );

        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SagaScriptBuilder.BuildCreateScript(saga, writer);
        }
        var script = builder.ToString();

        SqlValidator.Validate(script);
        Approvals.Verify(script);
    }

    [Test]
    public void CreateWithCorrelationAndTransitional()
    {
        var saga = new SagaDefinition(
            tableSuffix: "theSaga",
            name: "theSaga",
            correlationProperty: new CorrelationProperty
            {
                Name = "CorrelationProperty",
                Type = CorrelationMemberType.String
            },
            transitionalCorrelationProperty: new CorrelationProperty
            {
                Name = "TransitionalProperty",
                Type = CorrelationMemberType.String
            }
        );

        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SagaScriptBuilder.BuildCreateScript(saga, writer);
        }
        var script = builder.ToString();

        SqlValidator.Validate(script);
        Approvals.Verify(script);
    }

    [Test]
    public void BuildDropScript()
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            var saga = new SagaDefinition(
                tableSuffix: "theSaga",
                name: "theSaga"
            );
            SagaScriptBuilder.BuildDropScript(saga, writer);
        }
        var script = builder.ToString();
        SqlValidator.Validate(script);
        Approvals.Verify(script);
    }
}