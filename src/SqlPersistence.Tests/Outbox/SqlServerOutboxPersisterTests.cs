using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class SqlServerOutboxPersisterTests : OutboxPersisterTests
{
    public SqlServerOutboxPersisterTests() : base(BuildSqlVariant.MsSqlServer)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return MsSqlConnectionBuilder.Build;
    }
}