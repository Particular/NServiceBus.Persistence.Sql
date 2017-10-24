using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;

public class PostgreSqlSagaPersisterTests : SagaPersisterTests
{
    public PostgreSqlSagaPersisterTests() : base(BuildSqlDialect.PostgreSql, "SchemaName")
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return () =>
        {
            var connection = PostgreSqlConnectionBuilder.Build();
            connection.Open();
            return connection;
        };
    }
    protected override bool IsConcurrencyException(Exception innerException)
    {
        return innerException.Message.Contains("duplicate key value violates unique constraint");
    }

}