using System.IO;
using System.Text;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class SubscriptionScriptBuilderTest
{
    [Test]
    [TestCase(BuildSqlDialect.MsSqlServer)]
    [TestCase(BuildSqlDialect.MySql)]
    [TestCase(BuildSqlDialect.Oracle)]
    [TestCase(BuildSqlDialect.PostgreSql)]
    public void BuildCreateScript(BuildSqlDialect sqlDialect)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SubscriptionScriptBuilder.BuildCreateScript(writer, sqlDialect);
        }
        var script = builder.ToString();
        if (sqlDialect == BuildSqlDialect.MsSqlServer)
        {
            SqlValidator.Validate(script);
        }

        Approver.Verify(script, scenario: "ForScenario." + sqlDialect);
    }

    [Test]
    [TestCase(BuildSqlDialect.MsSqlServer)]
    [TestCase(BuildSqlDialect.MySql)]
    [TestCase(BuildSqlDialect.Oracle)]
    [TestCase(BuildSqlDialect.PostgreSql)]
    public void BuildDropScript(BuildSqlDialect sqlDialect)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SubscriptionScriptBuilder.BuildDropScript(writer, sqlDialect);
        }
        var script = builder.ToString();
        if (sqlDialect == BuildSqlDialect.MsSqlServer)
        {
            SqlValidator.Validate(script);
        }

        Approver.Verify(script, scenario: "ForScenario." + sqlDialect);
    }
}