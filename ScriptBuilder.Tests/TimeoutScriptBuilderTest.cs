using System.IO;
using System.Text;
using ApprovalTests;
using ApprovalTests.Namers;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class TimeoutScriptBuilderTest
{
    [Test]
    [TestCase(SqlVarient.MsSqlServer)]
    [TestCase(SqlVarient.MySql)]
    public void BuildCreateScript(SqlVarient sqlVarient)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            TimeoutScriptBuilder.BuildCreateScript(writer, sqlVarient);
        }
        var script = builder.ToString();
        if (sqlVarient != SqlVarient.MySql)
        {
            SqlValidator.Validate(script);
        }
        using (ApprovalResults.ForScenario(sqlVarient))
        {
            Approvals.Verify(script);
        }
    }

    [Test]
    [TestCase(SqlVarient.MsSqlServer)]
    [TestCase(SqlVarient.MySql)]
    public void BuildDropScript(SqlVarient sqlVarient)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            TimeoutScriptBuilder.BuildDropScript(writer, sqlVarient);
        }
        var script = builder.ToString();
        if (sqlVarient != SqlVarient.MySql)
        {
            SqlValidator.Validate(script);
        }
        using (ApprovalResults.ForScenario(sqlVarient))
        {
            Approvals.Verify(script);
        }
    }
}