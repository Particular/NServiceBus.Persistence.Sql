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
    [TestCase(SqlVarient.MsSqlServer)]
    public void BuildCreateScript(SqlVarient sqlVarient)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            OutboxScriptBuilder.BuildCreateScript(writer,sqlVarient);
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
            OutboxScriptBuilder.BuildDropScript(writer, sqlVarient);
        }
        var script = builder.ToString();
        SqlValidator.Validate(script);
        using (ApprovalResults.ForScenario(sqlVarient))
        {
            Approvals.Verify(script);
        }
    }
}