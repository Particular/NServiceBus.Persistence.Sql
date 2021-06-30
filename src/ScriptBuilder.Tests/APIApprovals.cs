using NUnit.Framework;
using Particular.Approvals;
using PublicApiGenerator;
using System.IO;
using System.Reflection;

[TestFixture]
public class APIApprovals
{
    [Test]
    public void Approve()
    {
        var combine = Path.Combine(TestContext.CurrentContext.TestDirectory, "NServiceBus.Persistence.Sql.ScriptBuilder.dll");
        var assembly = Assembly.LoadFile(combine);
        var publicApi = ApiGenerator.GeneratePublicApi(assembly, excludeAttributes: new[] { "System.Runtime.Versioning.TargetFrameworkAttribute", "System.Reflection.AssemblyMetadataAttribute" });
        Approver.Verify(publicApi);
    }
}