using System;
using System.Data.Common;
using System.Data.SqlClient;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class SqlServerSagaPersisterTests: SagaPersisterTests
{
    static string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=sqlpersistencetests;Integrated Security=True";

    public SqlServerSagaPersisterTests() : base(BuildSqlVarient.MsSqlServer)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return () =>
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        };
    }

    protected override bool IsConcurrencyException(Exception innerException)
    {
        return innerException.Message.Contains("Cannot insert duplicate key row in object ");
    }
}