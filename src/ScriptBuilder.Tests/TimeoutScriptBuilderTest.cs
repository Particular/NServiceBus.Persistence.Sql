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
    [TestCase(BuildSqlVariant.MsSqlServer)]
    [TestCase(BuildSqlVariant.MySql)]
    public void BuildCreateScript(BuildSqlVariant sqlVariant)
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            TimeoutScriptBuilder.BuildCreateScript(writer, sqlVariant);
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
            TimeoutScriptBuilder.BuildDropScript(writer, sqlVariant);
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
}