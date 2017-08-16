using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class OracleTimeoutPersisterTests : TimeoutPersisterTests
{
    public OracleTimeoutPersisterTests() : base(BuildSqlDialect.Oracle, null)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return OracleConnectionBuilder.Build;
    }

    protected override string GetTablePrefix()
    {
        var name = $"Test {TestContext.CurrentContext.Test.Name}";
        if (name.Length > 24)
        {
            name = name.Substring(0, 24);
        }
        return name.ToUpper();
    }
}