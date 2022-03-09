using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture, SqlServerTest]
public class SqlServerMicrosoftDataClientTimeoutPersisterTests : TimeoutPersisterTests
{
    public SqlServerMicrosoftDataClientTimeoutPersisterTests() : base(BuildSqlDialect.MsSqlServer, "schema_name")
    {
    }

    protected override Func<string, DbConnection> GetConnection()
    {
        return x => MsSqlMicrosoftDataClientConnectionBuilder.Build();
    }
}