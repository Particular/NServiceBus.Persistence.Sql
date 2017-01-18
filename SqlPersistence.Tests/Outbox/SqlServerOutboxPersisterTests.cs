using System;
using System.Data.Common;
using System.Data.SqlClient;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class SqlServerOutboxPersisterTests : OutboxPersisterTests
{
    public SqlServerOutboxPersisterTests() : base(BuildSqlVariant.MsSqlServer)
    {
    }

    string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=sqlpersistencetests;Integrated Security=True";
    protected override Func<DbConnection> GetConnection()
    {
        return () => new SqlConnection(connectionString);
    }
}