using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class MySqlOutboxPersisterTests : OutboxPersisterTests
{
    public MySqlOutboxPersisterTests() : base(BuildSqlDialect.MySql, null)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return MySqlConnectionBuilder.Build;
    }
}