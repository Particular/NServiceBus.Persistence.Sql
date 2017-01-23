using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class MySqlSagaPersisterTests: SagaPersisterTests
{
    public MySqlSagaPersisterTests() : base(BuildSqlVariant.MySql)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return () =>
        {
            var connection = MySqlConnectionBuilder.Build();
            connection.Open();
            return connection;
        };
    }
    protected override bool IsConcurrencyException(Exception innerException)
    {
        return innerException.Message.Contains("Duplicate entry ");
    }
}