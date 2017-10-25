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
        using (var connection = MySqlConnectionBuilder.Build())
        {
            connection.Open();
            var sql = $@"
select count(*)
from information_schema.columns
where table_schema = database() and
      column_name = '{propertyName}' and
      table_name = '{table}';
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
                    return reader.GetInt32(0) > 0;
                }
            }
        }

    }

    protected override bool IsConcurrencyException(Exception innerException)
    {
        return innerException.Message.Contains("Duplicate entry ");
    }

}