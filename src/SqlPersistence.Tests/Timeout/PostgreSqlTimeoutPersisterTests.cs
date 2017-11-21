using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class PostgreSqlTimeoutPersisterTests : TimeoutPersisterTests
{
    public PostgreSqlTimeoutPersisterTests() : base(BuildSqlDialect.PostgreSql, "SchemaName")
    {
    }

    protected override Func<string, DbConnection> GetConnection()
    {
        return x => PostgreSqlConnectionBuilder.Build();
    }
}