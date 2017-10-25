using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;

#if RELEASE
using NUnit.Framework;
// So this test does not run on CI as server install does not support unicode
[Explicit("MySqlUnicode")]
#endif
public class MySqlSagaPersisterTests: SagaPersisterTests
{
    public MySqlSagaPersisterTests() : base(BuildSqlDialect.MySql, null)
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

    protected override bool PropertyExists(string schema, string table, string propertyName)
    {
        throw new NotImplementedException();
    }

    protected override bool IsConcurrencyException(Exception innerException)
    {
        return innerException.Message.Contains("Duplicate entry ");
    }

}