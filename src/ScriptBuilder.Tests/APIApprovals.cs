using NUnit.Framework;
using PublicApiGenerator;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

[TestFixture]
public class APIApprovals
{
    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Approve()
    {
        var combine = Path.Combine(TestContext.CurrentContext.TestDirectory, "NServiceBus.Persistence.Sql.ScriptBuilder.dll");
        var assembly = Assembly.LoadFile(combine);
        var publicApi = ApiGenerator.GeneratePublicApi(assembly, excludeAttributes: new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" });
        TestApprover.Verify(publicApi);
    }
}