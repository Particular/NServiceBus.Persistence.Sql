using System.IO;
using System.Text;
using ApprovalTests;
using NServiceBus.Persistence.SqlServerXml;
using NUnit.Framework;

[TestFixture]
public class SagaScriptBuilderTest
{
    [Test]
    public void BuildCreateScript()
    {
        var saga = new SagaDefinition
        {
            Name = "theSaga",
            CorrelationMember = "Property1",
        };

        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SagaScriptBuilder.BuildCreateScript(saga, writer);
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
            SagaScriptBuilder.BuildDropScript("theSaga", writer);
        }
        var script = builder.ToString();
        SqlValidator.Validate(script);
        Approvals.Verify(script);
    }
}
