using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class PostgreSqlSubscriptionPersisterTests : SubscriptionPersisterTests
{
    public PostgreSqlSubscriptionPersisterTests() : base(BuildSqlDialect.PostgreSql, null)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return PostgreSqlConnectionBuilder.Build;
    }
}