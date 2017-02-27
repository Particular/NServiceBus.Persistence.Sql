using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class MySqlServerTimeoutPersisterTests : TimeoutPersisterTests
{
    public MySqlServerTimeoutPersisterTests() : base(BuildSqlVariant.MySql)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return MySqlConnectionBuilder.Build;
    }
}