using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class SqlServerSagaPersisterTests: SagaPersisterTests
{

    public SqlServerSagaPersisterTests() : base(BuildSqlVariant.MsSqlServer, "schema_name")
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return () =>
        {
            var connection = MsSqlConnectionBuilder.Build();
            connection.Open();
            return connection;
        };
    }

    protected override bool IsConcurrencyException(Exception innerException)
    {
        return innerException.Message.Contains("Cannot insert duplicate key row in object ");
    }
}