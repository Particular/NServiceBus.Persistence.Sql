using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class OracleSagaPersisterTests : SagaPersisterTests
{
    public OracleSagaPersisterTests() : base(BuildSqlVariant.Oracle, null)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return () =>
        {
            var connection = OracleConnectionBuilder.Build();
            connection.Open();
            return connection;
        };
    }

    protected override bool IsConcurrencyException(Exception innerException)
    {
        // TODO: Figure out what this value would be
        return innerException.Message.Contains("????????");
    }
}