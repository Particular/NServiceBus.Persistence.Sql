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

    protected override bool PropertyExists(string schema, string table, string propertyName)
    {
        using (var connection = PostgreSqlConnectionBuilder.Build())
        {
            connection.Open();
            var sql = $@"
select count(*)
from information_schema.columns
where
table_name = '{table}' and
column_name = '{propertyName}';
";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        return false;
                    }
                    if (!reader.Read())
                    {
                        return false;
                    }
                    var int32 = reader.GetInt32(0);
                    return int32 > 0;
                }
            }
        }
    }

    protected override bool IsConcurrencyException(Exception innerException)
    {
        return innerException.Message.Contains("duplicate key value violates unique constraint");
    }

}