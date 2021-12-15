using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture, MsSqlOnly]
public class SqlServerSystemDataClientSubscriptionPersisterTests : SubscriptionPersisterTests
{
    public SqlServerSystemDataClientSubscriptionPersisterTests() : base(BuildSqlDialect.MsSqlServer, "schema_name")
    {
    }

    protected override Func<string, DbConnection> GetConnection()
    {
        return x => MsSqlSystemDataClientConnectionBuilder.Build();
    }
}