using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class SqlServerSystemDataClientOutboxPersisterTests : OutboxPersisterTests
{
    public SqlServerSystemDataClientOutboxPersisterTests() : base(BuildSqlDialect.MsSqlServer, "schema_name")
    {
    }

    protected override Func<string, DbConnection> GetConnection()
    {
        return x => MsSqlSystemDataClientConnectionBuilder.Build();
    }
}