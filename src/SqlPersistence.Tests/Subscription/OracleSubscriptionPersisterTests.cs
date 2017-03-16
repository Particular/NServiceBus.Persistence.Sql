using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class OracleSubscriptionPersisterTests : SubscriptionPersisterTests
{
    public OracleSubscriptionPersisterTests() : base(BuildSqlVariant.Oracle, null)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return OracleConnectionBuilder.Build;
    }
}