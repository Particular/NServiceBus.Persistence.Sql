#if NET452
using System.Runtime.CompilerServices;
using ApprovalTests;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;
using PublicApiGenerator;

[TestFixture]
public class APIApprovals
{
    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Approve()
    {
        var publicApi = ApiGenerator.GeneratePublicApi(typeof(BuildSqlDialect).Assembly);
        Approvals.Verify(publicApi);
    }
}
#endif