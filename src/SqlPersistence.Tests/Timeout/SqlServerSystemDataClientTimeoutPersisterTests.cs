using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture, MsSqlOnly]
public class SqlServerSystemDataClientTimeoutPersisterTests : TimeoutPersisterTests
{
    public SqlServerSystemDataClientTimeoutPersisterTests() : base(BuildSqlDialect.MsSqlServer, "schema_name")
    {
    }

    protected override Func<string, DbConnection> GetConnection()
    {
        return x => MsSqlSystemDataClientConnectionBuilder.Build();
    }
}