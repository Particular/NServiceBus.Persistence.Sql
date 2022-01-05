using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture, MySqlTest]
public class MySqlServerTimeoutPersisterTests : TimeoutPersisterTests
{
    public MySqlServerTimeoutPersisterTests() : base(BuildSqlDialect.MySql, null)
    {
    }

    protected override bool SupportsSchemas() => false;

    protected override Func<string, DbConnection> GetConnection()
    {
        return x => MySqlConnectionBuilder.Build();
    }
}