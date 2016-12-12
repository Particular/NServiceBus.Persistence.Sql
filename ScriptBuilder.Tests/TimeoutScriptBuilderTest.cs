using System.IO;
using System.Text;
using ApprovalTests;
using ApprovalTests.Namers;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class TimeoutScriptBuilderTest
{
    [Test]
    [TestCase(BuildSqlVarient.MsSqlServer)]
    [TestCase(BuildSqlVarient.MySql)]
    public void BuildCreateScript(BuildSqlVarient sqlVarient)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            TimeoutScriptBuilder.BuildCreateScript(writer, sqlVarient);
        }
        var script = builder.ToString();
        if (sqlVarient != BuildSqlVarient.MySql)
        {
            SqlValidator.Validate(script);
        }
        using (ApprovalResults.ForScenario(sqlVarient))
        {
            Approvals.Verify(script);
        }
    }

    [Test]
    [TestCase(BuildSqlVarient.MsSqlServer)]
    [TestCase(BuildSqlVarient.MySql)]
    public void BuildDropScript(BuildSqlVarient sqlVarient)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            TimeoutScriptBuilder.BuildDropScript(writer, sqlVarient);
        }
        var script = builder.ToString();
        if (sqlVarient != BuildSqlVarient.MySql)
        {
            SqlValidator.Validate(script);
        }
        using (ApprovalResults.ForScenario(sqlVarient))
        {
            Approvals.Verify(script);
        }
    }
}