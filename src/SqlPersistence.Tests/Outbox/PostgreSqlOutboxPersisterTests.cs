using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class PostgreSqlOutboxPersisterTests : OutboxPersisterTests
{
    public PostgreSqlOutboxPersisterTests() : base(BuildSqlDialect.PostgreSql, null)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return PostgreSqlConnectionBuilder.Build;
    }
}