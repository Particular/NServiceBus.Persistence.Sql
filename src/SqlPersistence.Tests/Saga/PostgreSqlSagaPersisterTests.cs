namespace PostgreSql
{
    using System;
    using System.Data.Common;
    using NServiceBus.Persistence.Sql.ScriptBuilder;

    public class PostgreSqlSagaPersisterTests : SagaPersisterTests
    {
        public PostgreSqlSagaPersisterTests() : base(BuildSqlDialect.PostgreSql, "SchemaName")
        {
        }

        protected override Func<string, DbConnection> GetConnection()
        {
            return x =>
            {
                var connection = PostgreSqlConnectionBuilder.Build();
                connection.Open();
                return connection;
            };
        }

        protected override string GetPropertyWhereClauseExists(string schema, string table, string propertyName)
        {
            return $@"
select count(*)
from information_schema.columns
where
table_name = '{table}' and
column_name = '{propertyName}';
";
        }

        protected override bool IsConcurrencyException(Exception innerException)
        {
            return innerException.Message.Contains("duplicate key value violates unique constraint");
        }

    }
}