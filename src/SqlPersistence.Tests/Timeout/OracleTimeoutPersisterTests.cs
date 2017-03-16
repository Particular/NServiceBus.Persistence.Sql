using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class OracleTimeoutPersisterTests : TimeoutPersisterTests
{
    public OracleTimeoutPersisterTests() : base(BuildSqlVariant.Oracle, null)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return OracleConnectionBuilder.Build;
    }
}