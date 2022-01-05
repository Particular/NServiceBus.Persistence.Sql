using System;
using System.Data.Common;
using NServiceBus.Persistence.Sql.ScriptBuilder;

[MySqlTest]
public class MySqlSagaPersisterTests : SagaPersisterTests
{
    public MySqlSagaPersisterTests() : base(BuildSqlDialect.MySql, null)
    {
    }

    protected override bool SupportsSchemas() => false;

    protected override Func<string, DbConnection> GetConnection()
    {
        return x =>
        {
            var connection = MySqlConnectionBuilder.Build();
            connection.Open();
            return connection;
        };
    }

    protected override string GetPropertyWhereClauseExists(string schema, string table, string propertyName)
    {
        return $@"
select count(*)
from information_schema.columns
where table_schema = database() and
      column_name = '{propertyName}' and
      table_name = '{table}';
";
    }

    protected override bool IsConcurrencyException(Exception innerException)
    {
        return innerException.Message.Contains("Duplicate entry ");
    }

}