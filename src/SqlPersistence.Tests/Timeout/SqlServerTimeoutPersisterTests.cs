using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class SqlServerTimeoutPersisterTests : TimeoutPersisterTests
{

    public SqlServerTimeoutPersisterTests() : base(BuildSqlVariant.MsSqlServer, "dbo")
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return MsSqlConnectionBuilder.Build;
    }
}