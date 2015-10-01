using System.Collections.Generic;
using System.IO;
using System.Text;
using ApprovalTests;
using NServiceBus.SqlPersistence;
using NUnit.Framework;

[TestFixture]
public class SagaScriptBuilderTest
{
    [Test]
    public void BuildCreateScript()
    {
        var sagas = new List<SagaDefinition>
        {
            new SagaDefinition
            {
                Name = "theSaga",
                MappedProperties = new List<string>
                {
                    "Property1",
                    "Property2"
                }
            }
        };

        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SagaScriptBuilder.BuildCreateScript("theschema", "theendpointname", sagas, s => writer);
        }
        var script = builder.ToString();

        SqlValidator.Validate(script);
        Approvals.Verify(script);
    }

    [Test]
    public void BuildDropScript()
    {
        var sagas = new List<string>
        {
            "theSaga"
        };

        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            SagaScriptBuilder.BuildDropScript("theschema", "theendpointname", sagas, s => writer);
        }
        var script = builder.ToString();
        SqlValidator.Validate(script);
        Approvals.Verify(script);
    }
}
