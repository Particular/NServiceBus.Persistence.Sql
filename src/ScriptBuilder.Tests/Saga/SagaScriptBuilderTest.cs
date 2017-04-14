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
    [TestCase(BuildSqlVariant.MsSqlServer)]
    [TestCase(BuildSqlVariant.MySql)]
    public void CreateWithCorrelation(BuildSqlVariant sqlVariant)
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
            SagaScriptBuilder.BuildCreateScript(saga, sqlVariant, writer);
        }
        var script = builder.ToString();

        if (sqlVariant != BuildSqlVariant.MySql)
        {
            SqlValidator.Validate(script);
        }
        using (ApprovalResults.ForScenario(sqlVariant))
        {
            Approvals.Verify(script);
        }
    }

    [Test]
    [TestCase(BuildSqlVariant.MsSqlServer)]
    [TestCase(BuildSqlVariant.MySql)]
    public void CreateWithCorrelationAndTransitional(BuildSqlVariant sqlVariant)
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
            SagaScriptBuilder.BuildCreateScript(saga, sqlVariant, writer);
        }
        var script = builder.ToString();

        if (sqlVariant != BuildSqlVariant.MySql)
        {
            SqlValidator.Validate(script);
        }

        using (ApprovalResults.ForScenario(sqlVariant))
        {
            Approvals.Verify(script);
        }
    }

    [Test]
    [TestCase(BuildSqlVariant.MsSqlServer)]
    [TestCase(BuildSqlVariant.MySql)]
    public void BuildDropScript(BuildSqlVariant sqlVariant)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            var saga = new SagaDefinition(
                correlationProperty: new CorrelationProperty
                (
                    name: "CorrelationProperty",
                    type: CorrelationPropertyType.String
                ),
                tableSuffix: "theSaga",
                name: "theSaga"
            );
            SagaScriptBuilder.BuildDropScript(saga, sqlVariant, writer);
        }
        var script = builder.ToString();
        if (sqlVariant == BuildSqlVariant.MsSqlServer)
        {
            SqlValidator.Validate(script);
        }

        using (ApprovalResults.ForScenario(sqlVariant))
        {
            Approvals.Verify(script);
        }
    }
}