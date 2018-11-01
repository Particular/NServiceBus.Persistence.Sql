using System;
using System.Data.Common;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class PostgreSqlTimeoutPersisterTests : TimeoutPersisterTests
{
    public PostgreSqlTimeoutPersisterTests() : base(BuildSqlDialect.PostgreSql, "SchemaName")
    {
    }

    protected override Func<string, ContextBag, DbConnection> GetConnection()
    {
        return (x, context) => PostgreSqlConnectionBuilder.Build();
    }
}