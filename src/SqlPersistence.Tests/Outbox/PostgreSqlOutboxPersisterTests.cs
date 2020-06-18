using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture(false, false)]
[TestFixture(true, false)]
[TestFixture(false, true)]
[TestFixture(true, true)]
public class PostgreSqlOutboxPersisterTests : OutboxPersisterTests
{
    public PostgreSqlOutboxPersisterTests(bool pessimistic, bool transactionScope) 
        : base(BuildSqlDialect.PostgreSql, "SchemaName", pessimistic, transactionScope)
    {
    }

    protected override Func<string, DbConnection> GetConnection()
    {
        return x => PostgreSqlConnectionBuilder.Build();
    }
}