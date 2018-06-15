using System.IO;
using System.Text;
using ApprovalTests.Namers;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

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

        using (ApprovalResults.ForScenario(sqlDialect))
        {
            TestApprover.Verify(script);
        }
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
        using (ApprovalResults.ForScenario(sqlDialect))
        {
            TestApprover.Verify(script);
        }
    }
}