using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture(false, false)]
[TestFixture(true, false)]
[TestFixture(false, true)]
[TestFixture(true, true)]
public class MySqlOutboxPersisterTests : OutboxPersisterTests
{
    public MySqlOutboxPersisterTests(bool pessimistic, bool transactionScope) 
        : base(BuildSqlDialect.MySql, null, pessimistic, transactionScope)
    {
    }

    protected override bool SupportsSchemas() => false;

    protected override Func<string, DbConnection> GetConnection()
    {
        return x => MySqlConnectionBuilder.Build();
    }
}