using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class OracleOutboxPersisterTests : OutboxPersisterTests
{
    public OracleOutboxPersisterTests() : base(BuildSqlVariant.Oracle, null)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return OracleConnectionBuilder.Build;
    }

    protected override string GetTablePrefix()
    {
        return "OUTBOXPERSISTER";
    }

    protected override string GetTableSuffix()
    {
        return "_OD";
    }
}