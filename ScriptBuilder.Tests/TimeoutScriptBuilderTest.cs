using System.IO;
using System.Text;
using ApprovalTests;
using NServiceBus.Persistence.Sql;
using NUnit.Framework;

[TestFixture]
public class TimeoutScriptBuilderTest
{
    [Test]
    public void BuildCreateScript()
    {
        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            TimeoutScriptBuilder.BuildCreateScript(writer);
        }
        var script = builder.ToString();
        SqlValidator.Validate(script);
        Approvals.Verify(script);
    }

    [Test]
    public void BuildDropScript()
    {

        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            TimeoutScriptBuilder.BuildDropScript(writer);
        }
        var script = builder.ToString();
        SqlValidator.Validate(script);
        Approvals.Verify(script);
    }
}