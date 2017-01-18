using System;
using System.Data.Common;
using MySql.Data.MySqlClient;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class MyServerTimeoutPersisterTests : TimeoutPersisterTests
{
    static string connectionString = "server=localhost;user=root;database=sqlpersistencetests;port=3306;password=Password1;Allow User Variables=True";

    public MyServerTimeoutPersisterTests() : base(BuildSqlVariant.MySql)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return () => new MySqlConnection(connectionString);
    }
}