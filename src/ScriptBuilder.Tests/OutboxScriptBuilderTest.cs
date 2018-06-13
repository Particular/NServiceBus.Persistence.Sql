using System.IO;
using System.Text;
using ApprovalTests;
using ApprovalTests.Namers;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class OutboxScriptBuilderTest
{
    [Test]
    [TestCase(BuildSqlDialect.MsSqlServer)]
    [TestCase(BuildSqlDialect.MySql)]
    [TestCase(BuildSqlDialect.PostgreSql)]
    [TestCase(BuildSqlDialect.Oracle)]
    public void BuildCreateScript(BuildSqlDialect sqlDialect)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            OutboxScriptBuilder.BuildCreateScript(writer,sqlDialect);
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
    [TestCase(BuildSqlDialect.PostgreSql)]
    [TestCase(BuildSqlDialect.Oracle)]
    public void BuildDropScript(BuildSqlDialect sqlDialect)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            OutboxScriptBuilder.BuildDropScript(writer, sqlDialect);
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