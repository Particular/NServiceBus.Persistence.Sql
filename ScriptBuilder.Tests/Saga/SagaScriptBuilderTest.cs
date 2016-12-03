using System.IO;
using System.Text;
using ApprovalTests;
using ApprovalTests.Namers;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class SagaScriptBuilderTest
{
    [Test]
    [TestCase(SqlVarient.MsSqlServer)]
    public void CreateWithCorrelation(SqlVarient sqlVarient)
    {
        var saga = new SagaDefinition(
            name: "theSaga",
            tableSuffix: "theSaga",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            )
        );

        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SagaScriptBuilder.BuildCreateScript(saga, sqlVarient, writer);
        }
        var script = builder.ToString();

        SqlValidator.Validate(script);
        using (ApprovalResults.ForScenario(sqlVarient))
        {
            Approvals.Verify(script);
        }
    }

    [Test]
    [TestCase(SqlVarient.MsSqlServer)]
    public void CreateWithNoCorrelation(SqlVarient sqlVarient)
    {
        var saga = new SagaDefinition(
            tableSuffix: "theSaga",
            name: "theSaga"
        );

        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SagaScriptBuilder.BuildCreateScript(saga, sqlVarient, writer);
        }
        var script = builder.ToString();

        SqlValidator.Validate(script);

        using (ApprovalResults.ForScenario(sqlVarient))
        {
            Approvals.Verify(script);
        }
    }

    [Test]
    [TestCase(SqlVarient.MsSqlServer)]
    public void CreateWithCorrelationAndTransitional(SqlVarient sqlVarient)
    {
        var saga = new SagaDefinition(
            tableSuffix: "theSaga",
            name: "theSaga",
            correlationProperty: new CorrelationProperty
            (
                name: "CorrelationProperty",
                type: CorrelationPropertyType.String
            ),
            transitionalCorrelationProperty: new CorrelationProperty
            (
                name: "TransitionalProperty",
                type: CorrelationPropertyType.String
            )
        );

        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SagaScriptBuilder.BuildCreateScript(saga, sqlVarient, writer);
        }
        var script = builder.ToString();

        SqlValidator.Validate(script);

        using (ApprovalResults.ForScenario(sqlVarient))
        {
            Approvals.Verify(script);
        }
    }

    [Test]
    [TestCase(SqlVarient.MsSqlServer)]
    public void BuildDropScript(SqlVarient sqlVarient)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            var saga = new SagaDefinition(
                tableSuffix: "theSaga",
                name: "theSaga"
            );
            SagaScriptBuilder.BuildDropScript(saga, sqlVarient, writer);
        }
        var script = builder.ToString();
        SqlValidator.Validate(script);

        using (ApprovalResults.ForScenario(sqlVarient))
        {
            Approvals.Verify(script);
        }
    }
}