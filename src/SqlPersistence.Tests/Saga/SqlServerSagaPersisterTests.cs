using System;
using System.Data.Common;
using System.Data.SqlClient;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class SqlServerSagaPersisterTests: SagaPersisterTests
{
    public SqlServerSagaPersisterTests() : base(BuildSqlDialect.MsSqlServer, "schema_name")
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

    protected override bool PropertyExists(string schema, string table, string propertyName)
    {
        using (var connection = MsSqlConnectionBuilder.Build())
        {
            connection.Open();
            var sql = $@"
select 1 from sys.columns
where Name = N'{propertyName}'
and Object_ID = Object_ID(N'{schema}.{table}')
";
            using (var command = new SqlCommand(sql, connection))
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

    protected override bool IsConcurrencyException(Exception innerException)
    {
        return innerException.Message.Contains("Cannot insert duplicate key row in object ");
    }
}