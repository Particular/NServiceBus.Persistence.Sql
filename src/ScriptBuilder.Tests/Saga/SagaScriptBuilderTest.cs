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
    [TestCase(BuildSqlDialect.MsSqlServer)]
    [TestCase(BuildSqlDialect.MySql)]
    [TestCase(BuildSqlDialect.Oracle)]
    public void CreateWithCorrelation(BuildSqlDialect sqlDialect)
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
            SagaScriptBuilder.BuildCreateScript(saga, sqlDialect, writer);
        }
        var script = builder.ToString();

        if (sqlDialect == BuildSqlDialect.MsSqlServer)
        {
            SqlValidator.Validate(script);
        }
        using (ApprovalResults.ForScenario(sqlDialect))
        {
            Approvals.Verify(script);
        }
    }

    [Test]
    [TestCase(BuildSqlDialect.MsSqlServer)]
    [TestCase(BuildSqlDialect.MySql)]
    [TestCase(BuildSqlDialect.Oracle)]
    public void CreateWithCorrelationAndTransitional(BuildSqlDialect sqlDialect)
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
            SagaScriptBuilder.BuildCreateScript(saga, sqlDialect, writer);
        }
        var script = builder.ToString();

        if (sqlDialect == BuildSqlDialect.MsSqlServer)
        {
            SqlValidator.Validate(script);
        }

        using (ApprovalResults.ForScenario(sqlDialect))
        {
            Approvals.Verify(script);
        }
    }

    [Test]
    [TestCase(BuildSqlDialect.MsSqlServer)]
    [TestCase(BuildSqlDialect.MySql)]
    [TestCase(BuildSqlDialect.Oracle)]
    public void BuildDropScript(BuildSqlDialect sqlDialect)
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
            SagaScriptBuilder.BuildDropScript(saga, sqlDialect, writer);
        }
        var script = builder.ToString();
        if (sqlDialect == BuildSqlDialect.MsSqlServer)
        {
            SqlValidator.Validate(script);
        }

        using (ApprovalResults.ForScenario(sqlDialect))
        {
            Approvals.Verify(script);
        }
    }
}