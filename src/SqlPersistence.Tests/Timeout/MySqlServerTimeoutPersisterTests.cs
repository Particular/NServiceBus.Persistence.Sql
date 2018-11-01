using System;
using System.Data.Common;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class MySqlServerTimeoutPersisterTests : TimeoutPersisterTests
{
    public MySqlServerTimeoutPersisterTests() : base(BuildSqlDialect.MySql, null)
    {
    }

    protected override bool SupportsSchemas() => false;

    protected override Func<string, ContextBag, DbConnection> GetConnection()
    {
        return (x, context) => MySqlConnectionBuilder.Build();
    }
}