using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class SqlServerTimeoutPersisterTests : TimeoutPersisterTests
{
    public SqlServerTimeoutPersisterTests() : base(BuildSqlDialect.MsSqlServer, "schema_name")
    {
    }

    protected override Func<string, DbConnection> GetConnection()
    {
        return x => MsSqlSystemDataClientConnectionBuilder.Build();
    }
}