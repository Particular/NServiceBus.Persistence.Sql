using System;
using System.Data.Common;
using MySql.Data.MySqlClient;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class MySqlSagaPersisterTests: SagaPersisterTests
{
    static string connectionString = "server=localhost;user=root;database=sqlpersistencetests;port=3306;password=Password1;Allow User Variables=True";

    public MySqlSagaPersisterTests() : base(BuildSqlVarient.MySql)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return () =>
        {
            var connection = new MySqlConnection(connectionString);
            connection.Open();
            return connection;
        };
    }
    protected override bool IsConcurrencyException(Exception innerException)
    {
        return innerException.Message.Contains("Duplicate entry ");
    }
}